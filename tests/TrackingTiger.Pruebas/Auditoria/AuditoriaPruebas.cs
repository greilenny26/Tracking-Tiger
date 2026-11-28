using System.Reflection;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Tracking_Tiger.Core.Auditoria;
using Tracking_Tiger.Core.ControlAcceso;
using Tracking_Tiger.Core.Correo;
using Tracking_Tiger.Core.Documentos;
using Tracking_Tiger.Core.Permisos;
using Tracking_Tiger.Core.Reportes;

namespace TrackingTiger.Pruebas.Auditoria;

// Criterios A-01 a A-23 de docs/criterios-auditoria.md, sin levantar la aplicación (RD-12).
public sealed class AuditoriaPruebas : IDisposable
{
    private const string ContrasenaActual = "Clave-actual-2026";
    private const string ContrasenaNueva = "Clave-nueva-2026";

    private readonly BaseEnMemoria _base = new();
    private readonly HasherContrasenasPbkdf2 _hasher = new();
    private readonly ServicioSolicitudesPermiso _permisos;
    private readonly ServicioDocumentos _documentos;
    private readonly ServicioAdministracionUsuarios _usuarios;
    private readonly ServicioRecuperacionContrasena _recuperacion;
    private readonly ServicioSesion _sesion;
    private readonly ServicioConsultaAuditoria _consulta;

    public AuditoriaPruebas()
    {
        var contexto = _base.Contexto;
        _permisos = new ServicioSolicitudesPermiso(contexto, _base.Reloj);
        _documentos = new ServicioDocumentos(contexto, _base.Reloj, new AlmacenEnMemoria());
        _usuarios = new ServicioAdministracionUsuarios(contexto, _base.Reloj);
        _recuperacion = new ServicioRecuperacionContrasena(contexto, _base.Reloj, new ColaCorreo(contexto, _base.Reloj),
            _hasher, NullLogger<ServicioRecuperacionContrasena>.Instance);
        _sesion = new ServicioSesion(contexto, _hasher, _base.Reloj);
        _consulta = new ServicioConsultaAuditoria(contexto);
    }

    public void Dispose() => _base.Dispose();

    // A-01 y A-02 (RF-AUD-01, RF-AUD-02)
    [Fact]
    public async Task Aprobar_RegistraQuienAproboConTodosLosDatos()
    {
        // Preparar
        var admin = await _base.CrearAdministradorAsync("admin");
        var solicitud = await SolicitarAsync(await _base.CrearEstandarAsync("ana"));

        // Actuar
        await _permisos.AprobarAsync(solicitud, admin, 60);

        // Verificar
        var registro = await UnicoRegistroAsync();
        Assert.Equal(admin, registro.UsuarioId);
        Assert.Equal(AccionesAuditadas.AprobarSolicitud, registro.Accion);
        Assert.Equal(EntidadesAuditadas.SolicitudPermiso, registro.Entidad);
        Assert.Equal(solicitud, registro.EntidadId);
        Assert.Equal(_base.Reloj.AhoraUtc, registro.Fecha);
        Assert.Equal("Pendiente", registro.ValorAnterior);
        Assert.Equal("Aplicada", registro.ValorNuevo);
    }

    // A-03 (RF-AUD-02)
    [Fact]
    public async Task Rechazar_RegistraElMotivo()
    {
        // Preparar
        var admin = await _base.CrearAdministradorAsync("admin");
        var solicitud = await SolicitarAsync(await _base.CrearEstandarAsync("ana"));

        // Actuar
        await _permisos.RechazarAsync(solicitud, admin, "No corresponde a su puesto.");

        // Verificar
        var registro = await UnicoRegistroAsync();
        Assert.Equal(AccionesAuditadas.RechazarSolicitud, registro.Accion);
        Assert.Equal("Rechazada: No corresponde a su puesto.", registro.ValorNuevo);
    }

    // A-04 (RF-AUD-02)
    [Fact]
    public async Task ResolucionRechazada_NoCreaRegistro()
    {
        // Preparar
        var admin = await _base.CrearAdministradorAsync("admin");
        var solicitud = await SolicitarAsync(await _base.CrearEstandarAsync("ana"));
        await _permisos.AprobarAsync(solicitud, admin, 60);

        // Actuar
        await _permisos.AprobarAsync(solicitud, admin, 60);
        await _permisos.RechazarAsync(await SolicitarAsync(await _base.CrearEstandarAsync("luis")), admin, "");

        // Verificar
        Assert.Equal(1, await _base.Contexto.RegistrosAuditoria.CountAsync());
    }

    // A-05 y A-06 (RF-AUD-03, RF-DOC-06)
    [Fact]
    public async Task SubirYEliminarDocumento_GeneranUnRegistroCadaUno()
    {
        // Preparar
        var ana = await _base.CrearEstandarAsync("ana");

        // Actuar
        var subido = await SubirAsync(ana);
        await _documentos.EliminarAsync(subido, ana);

        // Verificar
        var registros = await _base.Contexto.RegistrosAuditoria.OrderBy(r => r.Id).ToListAsync();
        Assert.Equal(2, registros.Count);
        Assert.Equal((AccionesAuditadas.SubirDocumento, subido, "No existía", "Activo: expediente.pdf"),
            (registros[0].Accion, registros[0].EntidadId, registros[0].ValorAnterior, registros[0].ValorNuevo));
        Assert.Equal((AccionesAuditadas.EliminarDocumento, subido, "Activo", "Eliminado"),
            (registros[1].Accion, registros[1].EntidadId, registros[1].ValorAnterior, registros[1].ValorNuevo));
        Assert.All(registros, r => Assert.Equal(ana, r.UsuarioId));
    }

    // A-07 (RF-AUD-03)
    [Fact]
    public async Task OperacionDeDocumentoRechazada_NoCreaRegistro()
    {
        // Preparar
        var ana = await _base.CrearEstandarAsync("ana");
        var luis = await _base.CrearEstandarAsync("luis");
        var subido = await SubirAsync(ana);

        // Actuar
        await _documentos.EliminarAsync(subido, luis);
        await _documentos.SubirAsync(ana, "vacio.txt", "text/plain", 0, new MemoryStream());

        // Verificar: solo el de la subida válida.
        Assert.Equal(AccionesAuditadas.SubirDocumento, (await UnicoRegistroAsync()).Accion);
    }

    // A-08 (RF-AUD-04)
    [Fact]
    public async Task CambiarRol_RegistraElRolAnteriorYElNuevo()
    {
        // Preparar
        var admin = await _base.CrearAdministradorAsync("admin");
        var ana = await _base.CrearEstandarAsync("ana");

        // Actuar
        await _usuarios.CambiarRolAsync(ana, "Administrador", admin);

        // Verificar
        var registro = await UnicoRegistroAsync();
        Assert.Equal((admin, AccionesAuditadas.CambiarRol, EntidadesAuditadas.Usuario, ana, "Estándar", "Administrador"),
            (registro.UsuarioId, registro.Accion, registro.Entidad, registro.EntidadId, registro.ValorAnterior, registro.ValorNuevo));
    }

    // A-09 (RF-AUD-04)
    [Fact]
    public async Task CambioDeRolRechazado_NoCreaRegistro()
    {
        // Preparar
        var admin = await _base.CrearAdministradorAsync("admin");
        var ana = await _base.CrearEstandarAsync("ana");

        // Actuar
        await _usuarios.CambiarRolAsync(admin, "Estandar", admin);
        await _usuarios.CambiarRolAsync(ana, "Supervisor", admin);

        // Verificar
        Assert.Empty(await _base.Contexto.RegistrosAuditoria.ToListAsync());
    }

    // A-10 y A-13 (RF-AUD-05)
    [Fact]
    public async Task RestablecerConCodigo_LoRegistraComoIniciadoPorElUsuarioSinGuardarSecretos()
    {
        // Preparar
        var ana = await CrearConContrasenaAsync("ana");
        var codigo = await EmitirCodigoAsync(ana);

        // Actuar
        var resultado = await _recuperacion.RestablecerAsync(codigo, ContrasenaNueva);

        // Verificar
        Assert.Equal(EstadoRestablecimiento.Restablecida, resultado.Estado);
        var registro = await UnicoRegistroAsync();
        Assert.Equal((ana, AccionesAuditadas.RestablecerContrasena, ana, AccionesAuditadas.IniciadoPorUsuario),
            (registro.UsuarioId, registro.Accion, registro.EntidadId, registro.ValorAnterior));
        await NoGuardaSecretosAsync(ana, codigo, ContrasenaNueva);
    }

    // A-11 y A-13 (RF-AUD-05)
    [Fact]
    public async Task ForzarRestablecimiento_LoRegistraComoIniciadoPorElAdministrador()
    {
        // Preparar
        var admin = await _base.CrearAdministradorAsync("admin");
        var ana = await CrearConContrasenaAsync("ana");

        // Actuar
        await _recuperacion.ForzarRestablecimientoAsync(ana, admin);

        // Verificar
        var registro = await UnicoRegistroAsync();
        Assert.Equal((admin, AccionesAuditadas.ForzarRestablecimiento, ana, AccionesAuditadas.IniciadoPorAdministrador),
            (registro.UsuarioId, registro.Accion, registro.EntidadId, registro.ValorAnterior));
        await NoGuardaSecretosAsync(ana, ContrasenaActual);
    }

    // Decisión previa: el cambio de la propia contraseña también se audita (RF-AUD-05).
    [Fact]
    public async Task CambiarContrasenaPropia_LoRegistraComoIniciadoPorElUsuario()
    {
        // Preparar
        var ana = await CrearConContrasenaAsync("ana");

        // Actuar
        await _sesion.CambiarContrasenaAsync(ana, ContrasenaActual, ContrasenaNueva);

        // Verificar
        var registro = await UnicoRegistroAsync();
        Assert.Equal((ana, AccionesAuditadas.CambiarContrasenaPropia, AccionesAuditadas.IniciadoPorUsuario),
            (registro.UsuarioId, registro.Accion, registro.ValorAnterior));
        await NoGuardaSecretosAsync(ana, ContrasenaActual, ContrasenaNueva);
    }

    // A-12 (RF-AUD-05)
    [Fact]
    public async Task CodigoUsadoOVencido_NoCreaRegistro()
    {
        // Preparar
        var ana = await CrearConContrasenaAsync("ana");
        var usado = await EmitirCodigoAsync(ana);
        await _recuperacion.RestablecerAsync(usado, ContrasenaNueva);
        var vencido = await EmitirCodigoAsync(ana);
        _base.Reloj.AhoraUtc += CodigoRecuperacion.Vigencia + TimeSpan.FromMinutes(1);

        // Actuar
        await _recuperacion.RestablecerAsync(usado, "Otra-clave-2026");
        await _recuperacion.RestablecerAsync(vencido, "Otra-clave-2026");

        // Verificar: solo el restablecimiento válido.
        Assert.Equal(1, await _base.Contexto.RegistrosAuditoria.CountAsync());
    }

    // A-14 (RF-AUD-06)
    [Fact]
    public void LaAuditoriaSoloTieneOperacionesDeConsulta()
    {
        var verbos = typeof(ControladorAuditoria).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SelectMany(m => m.GetCustomAttributes<HttpMethodAttribute>())
            .SelectMany(a => a.HttpMethods)
            .Distinct()
            .ToList();

        Assert.Equal(["GET"], verbos);
        Assert.DoesNotContain(typeof(RegistroAuditoria).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
            m => !m.IsSpecialName);
    }

    // A-15 (RF-AUD-06)
    [Fact]
    public async Task ModificarOBorrarUnRegistro_SeRechazaYElRegistroNoCambia()
    {
        // Preparar
        var admin = await _base.CrearAdministradorAsync("admin");
        await _usuarios.CambiarRolAsync(await _base.CrearEstandarAsync("ana"), "Administrador", admin);
        var registro = await UnicoRegistroAsync();

        // Actuar
        _base.Contexto.Entry(registro).Property(r => r.ValorNuevo).CurrentValue = "Alterado";
        var alModificar = await Assert.ThrowsAsync<InvalidOperationException>(() => _base.Contexto.SaveChangesAsync());
        _base.Contexto.ChangeTracker.Clear();
        _base.Contexto.RegistrosAuditoria.Remove(await UnicoRegistroAsync());
        var alBorrar = await Assert.ThrowsAsync<InvalidOperationException>(() => _base.Contexto.SaveChangesAsync());
        _base.Contexto.ChangeTracker.Clear();

        // Verificar
        Assert.Contains("auditoría", alModificar.Message);
        Assert.Contains("auditoría", alBorrar.Message);
        Assert.Equal("Administrador", (await _base.Contexto.RegistrosAuditoria.AsNoTracking().SingleAsync()).ValorNuevo);
    }

    // A-16, A-17 y A-19 (RF-AUD-07)
    [Fact]
    public async Task Consulta_FiltraPorUsuarioYPorEntidad()
    {
        // Preparar
        var admin = await _base.CrearAdministradorAsync("admin");
        var ana = await _base.CrearEstandarAsync("ana");
        await _usuarios.CambiarRolAsync(await _base.CrearEstandarAsync("luis"), "Administrador", admin);
        await SubirAsync(ana);
        await SubirAsync(admin);

        // Actuar
        var deAna = await ConsultarAsync(usuarioId: ana.ToString());
        var documentos = await ConsultarAsync(entidad: "documento");
        var documentosDelAdmin = await ConsultarAsync(usuarioId: admin.ToString(), entidad: "Documento", desde: "2026-11-20", hasta: "2026-11-20");

        // Verificar
        Assert.Equal(ana, Assert.Single(deAna).UsuarioId);
        Assert.Equal(2, documentos.Count);
        Assert.All(documentos, r => Assert.Equal(EntidadesAuditadas.Documento, r.Entidad));
        var unico = Assert.Single(documentosDelAdmin);
        Assert.Equal((admin, EntidadesAuditadas.Documento, "admin"), (unico.UsuarioId, unico.Entidad, unico.NombreUsuario));
    }

    // A-18 y A-23 (RF-AUD-07)
    [Fact]
    public async Task Consulta_FiltraPorRangoDeDiasIncluyendoElUltimoSegundo()
    {
        // Preparar
        var ana = await _base.CrearEstandarAsync("ana");
        _base.Reloj.AhoraUtc = new DateTime(2026, 11, 20, 23, 59, 59, DateTimeKind.Utc);
        var delDia20 = await SubirAsync(ana);
        _base.Reloj.AhoraUtc = new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc);
        var delDia21 = await SubirAsync(ana);

        // Actuar y verificar
        Assert.Equal(delDia20, Assert.Single(await ConsultarAsync(desde: "2026-11-20", hasta: "2026-11-20")).EntidadId);
        Assert.Equal(delDia21, Assert.Single(await ConsultarAsync(desde: "2026-11-21", hasta: "2026-11-21")).EntidadId);
        Assert.Empty(await ConsultarAsync(desde: "2026-01-01", hasta: "2026-01-31"));
        Assert.Equal([delDia21, delDia20], (await ConsultarAsync()).Select(r => r.EntidadId));
    }

    // A-20 y A-21 (RF-AUD-07): la exigencia vive en el catálogo, que el filtro aplica en cada petición.
    [Fact]
    public void Consulta_EsSoloDelAdministrador()
    {
        Assert.Equal(ExigenciaAcceso.DeRol(Rol.Administrador), CatalogoOperaciones.Exigencias[CatalogoOperaciones.ConsultarAuditoria]);
    }

    // A-22 (RF-AUD-07, RD-07)
    [Theory]
    [InlineData("abc", null, null, null, "usuarioId")]
    [InlineData("-1", null, null, null, "usuarioId")]
    [InlineData(null, "Tarjeta", null, null, "entidad")]
    [InlineData(null, null, "20-11-2026", null, "desde")]
    [InlineData(null, null, "2026-11-21", "2026-11-20", "posterior")]
    public async Task Consulta_ConUnFiltroNoValido_DevuelveUnMensajeClaro(
        string? usuarioId, string? entidad, string? desde, string? hasta, string textoEsperado)
    {
        var (registros, error) = await _consulta.ConsultarAsync(usuarioId, entidad, desde, hasta);

        Assert.Null(registros);
        Assert.Contains(textoEsperado, error);
    }

    private async Task<IReadOnlyList<RegistroAuditoriaListado>> ConsultarAsync(
        string? usuarioId = null, string? entidad = null, string? desde = null, string? hasta = null)
    {
        var (registros, error) = await _consulta.ConsultarAsync(usuarioId, entidad, desde, hasta);
        Assert.Null(error);
        return registros!;
    }

    private Task<RegistroAuditoria> UnicoRegistroAsync() => _base.Contexto.RegistrosAuditoria.SingleAsync();

    private async Task<int> SolicitarAsync(int solicitante) =>
        (await _permisos.CrearAsync(solicitante, CatalogoOperaciones.ListarUsuarios)).Solicitud!.Id;

    private async Task<int> SubirAsync(int usuarioId) =>
        (await _documentos.SubirAsync(usuarioId, "expediente.pdf", "application/pdf", 3, new MemoryStream([1, 2, 3]))).Documento!.Id;

    private async Task<int> CrearConContrasenaAsync(string nombre)
    {
        var usuario = Usuario.CrearNuevo(nombre, $"{nombre}@prueba.local", _hasher.Hashear(ContrasenaActual), _base.Reloj.AhoraUtc);
        usuario.Activar();
        _base.Contexto.Usuarios.Add(usuario);
        await _base.Contexto.SaveChangesAsync();
        return usuario.Id;
    }

    // El código en claro solo lo conoce la prueba; en la base queda su hash, como en el flujo real.
    private async Task<string> EmitirCodigoAsync(int usuarioId)
    {
        var codigo = GeneradorTokens.GenerarToken();
        var usuario = await _base.Contexto.Usuarios.SingleAsync(u => u.Id == usuarioId);
        _base.Contexto.CodigosRecuperacion.Add(CodigoRecuperacion.Emitir(usuario, GeneradorTokens.CalcularHash(codigo), _base.Reloj.AhoraUtc));
        await _base.Contexto.SaveChangesAsync();
        return codigo;
    }

    // A-13: ningún campo del registro contiene contraseñas, su hash ni el código.
    private async Task NoGuardaSecretosAsync(int usuarioId, params string[] secretos)
    {
        var hash = await _base.Contexto.Usuarios.Where(u => u.Id == usuarioId).Select(u => u.HashContrasena).SingleAsync();
        var texto = string.Join("|", (await _base.Contexto.RegistrosAuditoria.AsNoTracking().ToListAsync())
            .Select(r => $"{r.Accion}|{r.Entidad}|{r.ValorAnterior}|{r.ValorNuevo}"));

        foreach (var secreto in secretos.Append(hash).Concat(secretos.Select(GeneradorTokens.CalcularHash)))
            Assert.DoesNotContain(secreto, texto);
    }

    private sealed class AlmacenEnMemoria : IAlmacenDocumentos
    {
        private readonly Dictionary<string, byte[]> _archivos = [];

        public async Task GuardarAsync(string ruta, Stream contenido, string tipo, CancellationToken cancelacion = default)
        {
            using var copia = new MemoryStream();
            await contenido.CopyToAsync(copia, cancelacion);
            _archivos[ruta] = copia.ToArray();
        }

        public Task<Stream?> AbrirAsync(string ruta, CancellationToken cancelacion = default) =>
            Task.FromResult<Stream?>(_archivos.TryGetValue(ruta, out var bytes) ? new MemoryStream(bytes) : null);
    }
}
