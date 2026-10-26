using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.ControlAcceso;
using Tracking_Tiger.Core.Permisos;
using Tracking_Tiger.Core.Persistencia;

namespace TrackingTiger.Pruebas.Permisos;

// Pruebas del servicio de Gestión de permisos (RD-12): se ejercita ServicioSolicitudesPermiso con una base
// SQLite en memoria, sin servidor y sin levantar la aplicación. xUnit crea una instancia de la clase por
// prueba, así que cada prueba tiene su propia base vacía: son independientes y pasan en cualquier orden.
public sealed class ServicioSolicitudesPermisoPruebas : IDisposable
{
    private const string Permiso = CatalogoOperaciones.ListarUsuarios;
    private static readonly DateTime Ahora = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _conexion;
    private readonly ContextoDatos _contexto;
    private readonly RelojFijo _reloj = new(Ahora);
    private readonly ServicioSolicitudesPermiso _servicio;

    public ServicioSolicitudesPermisoPruebas()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();
        var opciones = new DbContextOptionsBuilder<ContextoDatos>().UseSqlite(_conexion).Options;
        _contexto = new ContextoDatos(opciones);
        _contexto.Database.EnsureCreated();
        _servicio = new ServicioSolicitudesPermiso(_contexto, _reloj);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        _conexion.Dispose();
    }

    // ---------- RF-GP-01: crear ----------

    // RF-GP-01
    [Fact]
    public async Task CrearAsync_EstandarPideUnPermiso_LaSolicitudNaceEnPendiente()
    {
        // Preparar
        var estandar = await CrearEstandarAsync("ana");

        // Actuar
        var resultado = await _servicio.CrearAsync(estandar, Permiso);

        // Verificar
        Assert.Equal(EstadoCreacionSolicitud.Creada, resultado.Estado);
        Assert.Equal("Pendiente", resultado.Solicitud!.Estado);
        Assert.Equal(Ahora, resultado.Solicitud.FechaCreacion);
        var guardada = Leer(resultado.Solicitud.Id);
        Assert.Equal(EstadoSolicitud.Pendiente, guardada.Estado);
        Assert.Null(guardada.AprobadorId);
        Assert.Null(guardada.FechaResolucion);
    }

    // ---------- RF-GP-02: consultar ----------

    // RF-GP-02: un Estándar no ve las solicitudes de otros usuarios.
    [Fact]
    public async Task ListarDeUsuarioAsync_Estandar_NoVeLasSolicitudesDeOtrosUsuarios()
    {
        // Preparar
        var ana = await CrearEstandarAsync("ana");
        var luis = await CrearEstandarAsync("luis");
        var deAna = await CrearPendienteAsync(ana);
        await CrearPendienteAsync(luis);

        // Actuar
        var vistas = await _servicio.ListarDeUsuarioAsync(ana);

        // Verificar
        var unica = Assert.Single(vistas);
        Assert.Equal(deAna, unica.Id);
        Assert.Equal(ana, unica.SolicitanteId);
    }

    // RF-GP-02: el Administrador ve las pendientes de todos, y solo las pendientes.
    [Fact]
    public async Task ListarPendientesAsync_Administrador_VeLasPendientesDeTodosLosUsuarios()
    {
        // Preparar
        var admin = await CrearAdministradorAsync("admin");
        var ana = await CrearEstandarAsync("ana");
        var luis = await CrearEstandarAsync("luis");
        var marta = await CrearEstandarAsync("marta");
        var deAna = await CrearPendienteAsync(ana);
        var deLuis = await CrearPendienteAsync(luis);
        var deMarta = await CrearPendienteAsync(marta);
        await _servicio.RechazarAsync(deMarta, admin, "No corresponde.");

        // Actuar
        var pendientes = await _servicio.ListarPendientesAsync();

        // Verificar
        Assert.Equal([deAna, deLuis], pendientes.Select(s => s.Id));
        Assert.All(pendientes, s => Assert.Equal("Pendiente", s.Estado));
    }

    // ---------- RF-GP-03: aprobar ----------

    // RF-GP-03: queda registrado quién aprobó y cuándo.
    [Fact]
    public async Task AprobarAsync_Administrador_RegistraQuienAproboYCuando()
    {
        // Preparar
        var admin = await CrearAdministradorAsync("admin");
        var solicitud = await CrearPendienteAsync(await CrearEstandarAsync("ana"));
        var momentoAprobacion = Ahora.AddHours(2);
        _reloj.AhoraUtc = momentoAprobacion;

        // Actuar
        var resultado = await _servicio.AprobarAsync(solicitud, admin, 60);

        // Verificar
        Assert.Equal(EstadoResolucion.Resuelta, resultado.Estado);
        Assert.Equal(admin, resultado.Solicitud!.AprobadorId);
        Assert.Equal(momentoAprobacion, resultado.Solicitud.FechaResolucion);
        var guardada = Leer(solicitud);
        Assert.Equal(admin, guardada.AprobadorId);
        Assert.Equal(momentoAprobacion, guardada.FechaResolucion);
    }

    // RF-GP-03: solo un Administrador aprueba; un Estándar recibe un rechazo y nada cambia.
    [Fact]
    public async Task AprobarAsync_UsuarioEstandar_SeRechazaYLaSolicitudSiguePendiente()
    {
        // Preparar
        var solicitud = await CrearPendienteAsync(await CrearEstandarAsync("ana"));
        var otroEstandar = await CrearEstandarAsync("luis");

        // Actuar
        var resultado = await _servicio.AprobarAsync(solicitud, otroEstandar, 60);

        // Verificar
        Assert.Equal(EstadoResolucion.NoPermitida, resultado.Estado);
        var guardada = Leer(solicitud);
        Assert.Equal(EstadoSolicitud.Pendiente, guardada.Estado);
        Assert.Null(guardada.AprobadorId);
        Assert.Empty(_contexto.PermisosOtorgados);
    }

    // ---------- RF-GP-04: rechazar ----------

    // RF-GP-04
    [Fact]
    public async Task RechazarAsync_AdministradorConMotivo_QuedaRechazadaConElMotivo()
    {
        // Preparar
        var admin = await CrearAdministradorAsync("admin");
        var solicitud = await CrearPendienteAsync(await CrearEstandarAsync("ana"));

        // Actuar
        var resultado = await _servicio.RechazarAsync(solicitud, admin, "No lo necesita para su puesto.");

        // Verificar
        Assert.Equal(EstadoResolucion.Resuelta, resultado.Estado);
        var guardada = Leer(solicitud);
        Assert.Equal(EstadoSolicitud.Rechazada, guardada.Estado);
        Assert.Equal("No lo necesita para su puesto.", guardada.MotivoRechazo);
        Assert.Equal(admin, guardada.RechazadaPorId);
    }

    // RF-GP-04: sin motivo, ausente o vacío, el rechazo no se completa y el estado no cambia.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RechazarAsync_SinMotivo_SeRechazaYLaSolicitudSiguePendiente(string? motivo)
    {
        // Preparar
        var admin = await CrearAdministradorAsync("admin");
        var solicitud = await CrearPendienteAsync(await CrearEstandarAsync("ana"));

        // Actuar
        var resultado = await _servicio.RechazarAsync(solicitud, admin, motivo);

        // Verificar
        Assert.Equal(EstadoResolucion.NoPermitida, resultado.Estado);
        var guardada = Leer(solicitud);
        Assert.Equal(EstadoSolicitud.Pendiente, guardada.Estado);
        Assert.Null(guardada.MotivoRechazo);
        Assert.Null(guardada.RechazadaPorId);
    }

    // RF-GP-04: solo un Administrador rechaza.
    [Fact]
    public async Task RechazarAsync_UsuarioEstandar_SeRechazaYLaSolicitudSiguePendiente()
    {
        // Preparar
        var solicitud = await CrearPendienteAsync(await CrearEstandarAsync("ana"));
        var otroEstandar = await CrearEstandarAsync("luis");

        // Actuar
        var resultado = await _servicio.RechazarAsync(solicitud, otroEstandar, "No corresponde.");

        // Verificar
        Assert.Equal(EstadoResolucion.NoPermitida, resultado.Estado);
        Assert.Equal(EstadoSolicitud.Pendiente, Leer(solicitud).Estado);
    }

    // ---------- RF-GP-05: aplicación automática ----------

    // RF-GP-05: tras aprobar, la solicitud llega a Aplicada y el permiso pedido queda efectivo.
    [Fact]
    public async Task AprobarAsync_Pendiente_LlegaAAplicadaYElPermisoQuedaVigente()
    {
        // Preparar
        var admin = await CrearAdministradorAsync("admin");
        var ana = await CrearEstandarAsync("ana");
        var solicitud = await CrearPendienteAsync(ana);

        // Actuar
        var resultado = await _servicio.AprobarAsync(solicitud, admin, 60);

        // Verificar
        Assert.Equal("Aplicada", resultado.Solicitud!.Estado);
        Assert.Equal(EstadoSolicitud.Aplicada, Leer(solicitud).Estado);
        var permiso = Assert.Single(_contexto.PermisosOtorgados.AsNoTracking());
        Assert.Equal(ana, permiso.UsuarioId);
        Assert.Equal(Permiso, permiso.Operacion);
        Assert.Equal(solicitud, permiso.SolicitudId);
        Assert.Equal(Ahora.AddMinutes(60), permiso.FechaVencimiento);
        Assert.True(permiso.EstaVigente(Ahora));
        Assert.Equal(Ahora.AddMinutes(60), resultado.Solicitud.PermisoVigenteHasta);
    }

    // RF-GP-05: el permiso deja de servir solo cuando llega su vencimiento.
    [Fact]
    public async Task PermisoOtorgado_AlLlegarSuVencimiento_DejaDeEstarVigente()
    {
        // Preparar
        var admin = await CrearAdministradorAsync("admin");
        var solicitud = await CrearPendienteAsync(await CrearEstandarAsync("ana"));
        await _servicio.AprobarAsync(solicitud, admin, 60);
        var permiso = _contexto.PermisosOtorgados.AsNoTracking().Single();

        // Actuar
        var vigenteUnMinutoAntes = permiso.EstaVigente(Ahora.AddMinutes(59));
        var vigenteAlVencer = permiso.EstaVigente(Ahora.AddMinutes(60));

        // Verificar
        Assert.True(vigenteUnMinutoAntes);
        Assert.False(vigenteAlVencer);
    }

    // ---------- RF-GP-06: transiciones fuera de la tabla ----------

    // RF-GP-06: aprobar dos veces se rechaza; no cambia el aprobador ni se otorga un segundo permiso.
    [Fact]
    public async Task AprobarAsync_DosVeces_LaSegundaSeRechazaYNadaCambia()
    {
        // Preparar
        var admin = await CrearAdministradorAsync("admin");
        var otroAdmin = await CrearAdministradorAsync("admin2");
        var solicitud = await CrearPendienteAsync(await CrearEstandarAsync("ana"));
        await _servicio.AprobarAsync(solicitud, admin, 60);
        _reloj.AhoraUtc = Ahora.AddHours(1);

        // Actuar
        var resultado = await _servicio.AprobarAsync(solicitud, otroAdmin, 60);

        // Verificar
        Assert.Equal(EstadoResolucion.NoPermitida, resultado.Estado);
        var guardada = Leer(solicitud);
        Assert.Equal(EstadoSolicitud.Aplicada, guardada.Estado);
        Assert.Equal(admin, guardada.AprobadorId);
        Assert.Equal(Ahora, guardada.FechaResolucion);
        Assert.Single(_contexto.PermisosOtorgados);
    }

    // RF-GP-06: una solicitud ya Aplicada no puede rechazarse.
    [Fact]
    public async Task RechazarAsync_SolicitudAplicada_SeRechazaYSigueAplicada()
    {
        // Preparar
        var admin = await CrearAdministradorAsync("admin");
        var solicitud = await CrearPendienteAsync(await CrearEstandarAsync("ana"));
        await _servicio.AprobarAsync(solicitud, admin, 60);

        // Actuar
        var resultado = await _servicio.RechazarAsync(solicitud, admin, "Me equivoqué al aprobar.");

        // Verificar
        Assert.Equal(EstadoResolucion.NoPermitida, resultado.Estado);
        var guardada = Leer(solicitud);
        Assert.Equal(EstadoSolicitud.Aplicada, guardada.Estado);
        Assert.Null(guardada.MotivoRechazo);
        Assert.Null(guardada.RechazadaPorId);
    }

    // ---------- RF-GP-07: nadie resuelve su propia solicitud ----------

    // RF-GP-07
    [Fact]
    public async Task AprobarAsync_AdministradorSobreSuPropiaSolicitud_SeRechazaYSiguePendiente()
    {
        // Preparar
        var admin = await CrearAdministradorAsync("admin");
        var solicitud = await CrearPendienteAsync(admin);

        // Actuar
        var resultado = await _servicio.AprobarAsync(solicitud, admin, 60);

        // Verificar
        Assert.Equal(EstadoResolucion.NoPermitida, resultado.Estado);
        Assert.Equal(EstadoSolicitud.Pendiente, Leer(solicitud).Estado);
        Assert.Empty(_contexto.PermisosOtorgados);
    }

    // RF-GP-07
    [Fact]
    public async Task RechazarAsync_AdministradorSobreSuPropiaSolicitud_SeRechazaYSiguePendiente()
    {
        // Preparar
        var admin = await CrearAdministradorAsync("admin");
        var solicitud = await CrearPendienteAsync(admin);

        // Actuar
        var resultado = await _servicio.RechazarAsync(solicitud, admin, "Ya no la necesito.");

        // Verificar
        Assert.Equal(EstadoResolucion.NoPermitida, resultado.Estado);
        Assert.Equal(EstadoSolicitud.Pendiente, Leer(solicitud).Estado);
    }

    // ---------- Datos de prueba ----------

    private async Task<int> CrearEstandarAsync(string nombre) =>
        await GuardarUsuarioAsync(Usuario.CrearNuevo(nombre, $"{nombre}@prueba.local", "hash", Ahora));

    private async Task<int> CrearAdministradorAsync(string nombre) =>
        await GuardarUsuarioAsync(Usuario.CrearAdministradorInicial(nombre, $"{nombre}@prueba.local", "hash", Ahora));

    private async Task<int> GuardarUsuarioAsync(Usuario usuario)
    {
        _contexto.Usuarios.Add(usuario);
        await _contexto.SaveChangesAsync();
        return usuario.Id;
    }

    // Se guarda directamente (sin CrearAsync) para poder dar una solicitud también a un Administrador.
    private async Task<int> CrearPendienteAsync(int solicitanteId)
    {
        var solicitud = SolicitudPermiso.Crear(solicitanteId, Permiso, Ahora);
        _contexto.SolicitudesPermiso.Add(solicitud);
        await _contexto.SaveChangesAsync();
        return solicitud.Id;
    }

    // Lee la fila tal como quedó en la base, no la copia en memoria del contexto.
    private SolicitudPermiso Leer(int id) => _contexto.SolicitudesPermiso.AsNoTracking().Single(s => s.Id == id);

    private sealed class RelojFijo(DateTime ahoraUtc) : IReloj
    {
        public DateTime AhoraUtc { get; set; } = ahoraUtc;
    }
}
