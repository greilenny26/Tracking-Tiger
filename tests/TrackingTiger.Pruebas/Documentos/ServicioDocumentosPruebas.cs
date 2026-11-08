using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.ControlAcceso;
using Tracking_Tiger.Core.Documentos;
using Tracking_Tiger.Core.Persistencia;

namespace TrackingTiger.Pruebas.Documentos;

// Manejador de documentos (RF-DOC-01 a 04) sobre una base SQLite en memoria, sin levantar la aplicación.
// El almacén de archivos es uno en memoria: estas pruebas verifican las reglas del servicio, no SeaweedFS.
// xUnit crea una instancia por prueba: cada una tiene su propia base vacía y son independientes.
public sealed class ServicioDocumentosPruebas : IDisposable
{
    private static readonly DateTime Ahora = new(2026, 11, 8, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _conexion;
    private readonly ContextoDatos _contexto;
    private readonly AlmacenEnMemoria _almacen = new();
    private readonly ServicioDocumentos _servicio;

    public ServicioDocumentosPruebas()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();
        var opciones = new DbContextOptionsBuilder<ContextoDatos>().UseSqlite(_conexion).Options;
        _contexto = new ContextoDatos(opciones);
        _contexto.Database.EnsureCreated();
        _servicio = new ServicioDocumentos(_contexto, new RelojFijo(Ahora), _almacen);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        _conexion.Dispose();
    }

    // RF-DOC-01
    [Fact]
    public async Task SubirAsync_ConUnArchivoValido_RegistraNombreTipoTamanoFechaYQuienLoSubio()
    {
        // Preparar
        var dueno = await CrearEstandarAsync("ana");

        // Actuar
        var resultado = await SubirAsync(dueno, "informe.pdf", "application/pdf", "contenido del informe");

        // Verificar
        Assert.Equal(EstadoOperacionDocumento.Exito, resultado.Estado);
        var guardado = await _contexto.Documentos.AsNoTracking().SingleAsync();
        Assert.Equal("informe.pdf", guardado.Nombre);
        Assert.Equal("application/pdf", guardado.Tipo);
        Assert.Equal(Encoding.UTF8.GetByteCount("contenido del informe"), guardado.Tamano);
        Assert.Equal(Ahora, guardado.FechaSubida);
        Assert.Equal(dueno, guardado.UsuarioId);
        Assert.False(guardado.Eliminado);
        Assert.True(_almacen.Existe(guardado.RutaAlmacenamiento));
    }

    // RF-DOC-01: el registro queda consultable con todos sus datos.
    [Fact]
    public async Task ConsultarAsync_ElDueno_VeTodosLosDatosDelRegistro()
    {
        // Preparar
        var dueno = await CrearEstandarAsync("ana");
        var id = (await SubirAsync(dueno, "informe.pdf", "application/pdf", "abc")).Documento!.Id;

        // Actuar
        var resultado = await _servicio.ConsultarAsync(id, dueno);

        // Verificar
        Assert.Equal(EstadoOperacionDocumento.Exito, resultado.Estado);
        var detalle = resultado.Documento!;
        Assert.Equal(("informe.pdf", "application/pdf", 3L, Ahora, dueno, "ana"),
            (detalle.Nombre, detalle.Tipo, detalle.Tamano, detalle.FechaSubida, detalle.UsuarioId, detalle.SubidoPor));
    }

    // RF-DOC-01: el nombre que manda el usuario nunca decide la ruta; las carpetas se descartan.
    [Fact]
    public async Task SubirAsync_ConCarpetasEnElNombre_GuardaSoloElNombreDelArchivo()
    {
        // Preparar
        var dueno = await CrearEstandarAsync("ana");

        // Actuar
        var resultado = await SubirAsync(dueno, "../../secreto.txt", "text/plain", "abc");

        // Verificar
        Assert.Equal(EstadoOperacionDocumento.Exito, resultado.Estado);
        var guardado = await _contexto.Documentos.AsNoTracking().SingleAsync();
        Assert.Equal("secreto.txt", guardado.Nombre);
        Assert.StartsWith("documentos/", guardado.RutaAlmacenamiento);
        Assert.DoesNotContain("secreto", guardado.RutaAlmacenamiento);
    }

    // RD-07: un archivo ausente se rechaza con un mensaje controlado; no se guarda nada.
    [Fact]
    public async Task SubirAsync_SinArchivo_SeRechazaYNoSeGuardaNada()
    {
        // Preparar
        var dueno = await CrearEstandarAsync("ana");

        // Actuar
        var resultado = await _servicio.SubirAsync(dueno, null, null, 0, null);

        // Verificar
        Assert.Equal(EstadoOperacionDocumento.DatoInvalido, resultado.Estado);
        Assert.Empty(_contexto.Documentos);
        Assert.Equal(0, _almacen.Cantidad);
    }

    // RD-07: un archivo vacío se rechaza con un mensaje controlado; no se guarda nada.
    [Fact]
    public async Task SubirAsync_ConUnArchivoVacio_SeRechazaYNoSeGuardaNada()
    {
        // Preparar
        var dueno = await CrearEstandarAsync("ana");

        // Actuar
        var resultado = await SubirAsync(dueno, "vacio.txt", "text/plain", "");

        // Verificar
        Assert.Equal(EstadoOperacionDocumento.DatoInvalido, resultado.Estado);
        Assert.Empty(_contexto.Documentos);
        Assert.Equal(0, _almacen.Cantidad);
    }

    // RD-07: un archivo que supera el máximo se rechaza antes de tocar el almacén.
    [Fact]
    public async Task SubirAsync_ConUnArchivoDemasiadoGrande_SeRechazaYNoSeGuardaNada()
    {
        // Preparar
        var dueno = await CrearEstandarAsync("ana");
        using var contenido = new MemoryStream([1, 2, 3]);

        // Actuar
        var resultado = await _servicio.SubirAsync(dueno, "grande.bin", "application/octet-stream",
            ServicioDocumentos.TamanoMaximoBytes + 1, contenido);

        // Verificar
        Assert.Equal(EstadoOperacionDocumento.DatoInvalido, resultado.Estado);
        Assert.Empty(_contexto.Documentos);
        Assert.Equal(0, _almacen.Cantidad);
    }

    // RD-07, RD-08: si el almacén no responde, rechazo controlado y no queda un registro sin archivo.
    [Fact]
    public async Task SubirAsync_ConElAlmacenCaido_DevuelveAlmacenNoDisponibleYNoRegistraNada()
    {
        // Preparar
        var dueno = await CrearEstandarAsync("ana");
        _almacen.Caido = true;

        // Actuar
        var resultado = await SubirAsync(dueno, "informe.pdf", "application/pdf", "abc");

        // Verificar
        Assert.Equal(EstadoOperacionDocumento.AlmacenNoDisponible, resultado.Estado);
        Assert.Empty(_contexto.Documentos);
    }

    // RF-DOC-02: un usuario Estándar ve solo los suyos.
    [Fact]
    public async Task ListarAsync_UnUsuarioEstandar_VeSoloSusDocumentos()
    {
        // Preparar
        var ana = await CrearEstandarAsync("ana");
        var beto = await CrearEstandarAsync("beto");
        await SubirAsync(ana, "de-ana.txt", "text/plain", "a");
        await SubirAsync(beto, "de-beto.txt", "text/plain", "b");

        // Actuar
        var listado = await _servicio.ListarAsync(ana);

        // Verificar
        Assert.Equal(["de-ana.txt"], listado.Select(d => d.Nombre));
    }

    // RF-DOC-02: el Administrador ve los de todos.
    [Fact]
    public async Task ListarAsync_ElAdministrador_VeLosDocumentosDeTodos()
    {
        // Preparar
        var ana = await CrearEstandarAsync("ana");
        var beto = await CrearEstandarAsync("beto");
        var admin = await CrearAdministradorAsync();
        await SubirAsync(ana, "de-ana.txt", "text/plain", "a");
        await SubirAsync(beto, "de-beto.txt", "text/plain", "b");

        // Actuar
        var listado = await _servicio.ListarAsync(admin);

        // Verificar
        Assert.Equal(["de-ana.txt", "de-beto.txt"], listado.Select(d => d.Nombre).Order());
    }

    // RF-DOC-02: los eliminados no aparecen, ni para el dueño ni para el Administrador.
    [Fact]
    public async Task ListarAsync_ConUnDocumentoEliminado_NoLoIncluyeNiParaElAdministrador()
    {
        // Preparar
        var ana = await CrearEstandarAsync("ana");
        var admin = await CrearAdministradorAsync();
        var id = (await SubirAsync(ana, "borrado.txt", "text/plain", "a")).Documento!.Id;
        await SubirAsync(ana, "vigente.txt", "text/plain", "b");
        await _servicio.EliminarAsync(id, ana);

        // Actuar
        var deAna = await _servicio.ListarAsync(ana);
        var deAdmin = await _servicio.ListarAsync(admin);

        // Verificar
        Assert.Equal(["vigente.txt"], deAna.Select(d => d.Nombre));
        Assert.Equal(["vigente.txt"], deAdmin.Select(d => d.Nombre));
    }

    // RF-DOC-03: el dueño descarga su documento con su contenido.
    [Fact]
    public async Task DescargarAsync_ElDueno_RecibeElContenido()
    {
        // Preparar
        var ana = await CrearEstandarAsync("ana");
        var id = (await SubirAsync(ana, "nota.txt", "text/plain", "hola mundo")).Documento!.Id;

        // Actuar
        var resultado = await _servicio.DescargarAsync(id, ana);

        // Verificar
        Assert.Equal(EstadoOperacionDocumento.Exito, resultado.Estado);
        Assert.Equal("nota.txt", resultado.Descarga!.Nombre);
        Assert.Equal("hola mundo", await LeerAsync(resultado.Descarga.Contenido));
    }

    // RF-DOC-03: el Administrador descarga el documento de otro usuario.
    [Fact]
    public async Task DescargarAsync_ElAdministrador_DescargaElDocumentoDeOtroUsuario()
    {
        // Preparar
        var ana = await CrearEstandarAsync("ana");
        var admin = await CrearAdministradorAsync();
        var id = (await SubirAsync(ana, "nota.txt", "text/plain", "hola")).Documento!.Id;

        // Actuar
        var resultado = await _servicio.DescargarAsync(id, admin);

        // Verificar
        Assert.Equal(EstadoOperacionDocumento.Exito, resultado.Estado);
        Assert.Equal("hola", await LeerAsync(resultado.Descarga!.Contenido));
    }

    // RF-DOC-03, RD-06: otro usuario Estándar recibe un rechazo, aunque llame al servicio directamente.
    [Fact]
    public async Task DescargarAsync_OtroUsuarioEstandar_SeRechaza()
    {
        // Preparar
        var ana = await CrearEstandarAsync("ana");
        var beto = await CrearEstandarAsync("beto");
        var id = (await SubirAsync(ana, "nota.txt", "text/plain", "hola")).Documento!.Id;

        // Actuar
        var resultado = await _servicio.DescargarAsync(id, beto);

        // Verificar
        Assert.Equal(EstadoOperacionDocumento.SinPermiso, resultado.Estado);
        Assert.Null(resultado.Descarga);
    }

    // RF-DOC-03: un documento eliminado ya no se descarga.
    [Fact]
    public async Task DescargarAsync_UnDocumentoEliminado_DevuelveNoEncontrado()
    {
        // Preparar
        var ana = await CrearEstandarAsync("ana");
        var id = (await SubirAsync(ana, "nota.txt", "text/plain", "hola")).Documento!.Id;
        await _servicio.EliminarAsync(id, ana);

        // Actuar
        var resultado = await _servicio.DescargarAsync(id, ana);

        // Verificar
        Assert.Equal(EstadoOperacionDocumento.NoEncontrado, resultado.Estado);
    }

    // RF-DOC-04: la eliminación es lógica: el registro y el archivo siguen existiendo.
    [Fact]
    public async Task EliminarAsync_ElDueno_MarcaEliminadoSinBorrarElRegistroNiElArchivo()
    {
        // Preparar
        var ana = await CrearEstandarAsync("ana");
        var id = (await SubirAsync(ana, "nota.txt", "text/plain", "hola")).Documento!.Id;

        // Actuar
        var resultado = await _servicio.EliminarAsync(id, ana);

        // Verificar
        Assert.Equal(EstadoOperacionDocumento.Exito, resultado.Estado);
        var registro = await _contexto.Documentos.AsNoTracking().SingleAsync(d => d.Id == id);
        Assert.True(registro.Eliminado);
        Assert.Equal(Ahora, registro.FechaEliminacion);
        Assert.True(_almacen.Existe(registro.RutaAlmacenamiento));
    }

    // RF-DOC-04: el Administrador elimina el documento de otro usuario.
    [Fact]
    public async Task EliminarAsync_ElAdministrador_EliminaElDocumentoDeOtroUsuario()
    {
        // Preparar
        var ana = await CrearEstandarAsync("ana");
        var admin = await CrearAdministradorAsync();
        var id = (await SubirAsync(ana, "nota.txt", "text/plain", "hola")).Documento!.Id;

        // Actuar
        var resultado = await _servicio.EliminarAsync(id, admin);

        // Verificar
        Assert.Equal(EstadoOperacionDocumento.Exito, resultado.Estado);
        Assert.True((await _contexto.Documentos.AsNoTracking().SingleAsync(d => d.Id == id)).Eliminado);
    }

    // RF-DOC-04, RD-06: otro usuario Estándar no puede eliminar; el documento sigue en el listado del dueño.
    [Fact]
    public async Task EliminarAsync_OtroUsuarioEstandar_SeRechazaYElDocumentoSigueVisible()
    {
        // Preparar
        var ana = await CrearEstandarAsync("ana");
        var beto = await CrearEstandarAsync("beto");
        var id = (await SubirAsync(ana, "nota.txt", "text/plain", "hola")).Documento!.Id;

        // Actuar
        var resultado = await _servicio.EliminarAsync(id, beto);

        // Verificar
        Assert.Equal(EstadoOperacionDocumento.SinPermiso, resultado.Estado);
        Assert.False((await _contexto.Documentos.AsNoTracking().SingleAsync(d => d.Id == id)).Eliminado);
        Assert.Single(await _servicio.ListarAsync(ana));
    }

    private Task<ResultadoOperacionDocumento> SubirAsync(int usuarioId, string nombre, string tipo, string texto)
    {
        var bytes = Encoding.UTF8.GetBytes(texto);
        return _servicio.SubirAsync(usuarioId, nombre, tipo, bytes.Length, new MemoryStream(bytes));
    }

    private Task<int> CrearEstandarAsync(string nombre) =>
        CrearUsuarioAsync(Usuario.CrearNuevo(nombre, $"{nombre}@prueba.local", "hash", Ahora));

    private Task<int> CrearAdministradorAsync() =>
        CrearUsuarioAsync(Usuario.CrearAdministradorInicial("admin", "admin@prueba.local", "hash", Ahora));

    private async Task<int> CrearUsuarioAsync(Usuario usuario)
    {
        _contexto.Usuarios.Add(usuario);
        await _contexto.SaveChangesAsync();
        return usuario.Id;
    }

    private static async Task<string> LeerAsync(Stream contenido)
    {
        using var lector = new StreamReader(contenido);
        return await lector.ReadToEndAsync();
    }

    private sealed class RelojFijo(DateTime ahoraUtc) : IReloj
    {
        public DateTime AhoraUtc { get; } = ahoraUtc;
    }

    // Almacén en memoria. Caido = true simula que SeaweedFS no responde.
    private sealed class AlmacenEnMemoria : IAlmacenDocumentos
    {
        private readonly Dictionary<string, byte[]> _archivos = [];

        public bool Caido { get; set; }
        public int Cantidad => _archivos.Count;
        public bool Existe(string ruta) => _archivos.ContainsKey(ruta);

        public async Task GuardarAsync(string ruta, Stream contenido, string tipo, CancellationToken cancelacion = default)
        {
            if (Caido)
                throw new ErrorAlmacenDocumentos("El almacén de documentos no está disponible.");

            using var copia = new MemoryStream();
            await contenido.CopyToAsync(copia, cancelacion);
            _archivos[ruta] = copia.ToArray();
        }

        public Task<Stream?> AbrirAsync(string ruta, CancellationToken cancelacion = default)
        {
            if (Caido)
                throw new ErrorAlmacenDocumentos("El almacén de documentos no está disponible.");

            return Task.FromResult<Stream?>(_archivos.TryGetValue(ruta, out var bytes) ? new MemoryStream(bytes) : null);
        }
    }
}
