using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.ControlAcceso;
using Tracking_Tiger.Core.Permisos;
using Tracking_Tiger.Core.Persistencia;
using Tracking_Tiger.Negocio.Tarjetas;

namespace TrackingTiger.Pruebas.Tarjetas;

// Prueba de integración: el módulo Tarjetas y Gestión de permisos REALES (ServicioTarjetas y
// ServicioSolicitudesPermiso, sin dobles), sobre una base SQLite en memoria y sin levantar la aplicación.
// Levantar un Bloqueo Total exige un permiso levantar-bloqueo-total aprobado por un Administrador.
// xUnit crea una instancia por prueba: cada una tiene su propia base vacía y son independientes.
public sealed class IntegracionTarjetasPermisosPruebas : IDisposable
{
    private const string Nota = "Validado por llamada: el cliente respondió las preguntas de seguridad.";
    private static readonly DateTime Ahora = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _conexion;
    private readonly ContextoDatos _contexto;
    private readonly ServicioSolicitudesPermiso _permisos;
    private readonly ServicioTarjetas _tarjetas;

    public IntegracionTarjetasPermisosPruebas()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();
        var opciones = new DbContextOptionsBuilder<ContextoDatos>().UseSqlite(_conexion).Options;
        _contexto = new ContextoDatos(opciones);
        _contexto.Database.EnsureCreated();
        var reloj = new RelojFijo(Ahora);
        _permisos = new ServicioSolicitudesPermiso(_contexto, reloj);
        _tarjetas = new ServicioTarjetas(_contexto, reloj, _permisos);
    }

    public void Dispose()
    {
        _contexto.Dispose();
        _conexion.Dispose();
    }

    // Cara 1: mientras la solicitud no está aprobada, levantar el Bloqueo Total se rechaza.
    [Fact]
    public async Task LevantarBloqueoTotalAsync_ConLaSolicitudPendiente_SeRechazaYLaTarjetaSigueEnBloqueoTotal()
    {
        // Preparar
        var agente = await CrearUsuarioAsync(Usuario.CrearNuevo("agente", "agente@prueba.local", "hash", Ahora));
        var tarjeta = await TarjetaEnBloqueoTotalAsync(agente);
        var solicitud = await _permisos.CrearAsync(agente, CatalogoOperaciones.LevantarBloqueoTotal);
        Assert.Equal(EstadoCreacionSolicitud.Creada, solicitud.Estado);

        // Actuar
        var resultado = await _tarjetas.LevantarBloqueoTotalAsync(tarjeta, agente, Nota);

        // Verificar
        Assert.Equal(EstadoOperacionTarjeta.SinPermiso, resultado.Estado);
        Assert.Equal(EstadoTarjeta.BloqueoTotal, LeerEstado(tarjeta));
    }

    // Cara 2: después de que un Administrador aprueba la solicitud, el agente levanta el Bloqueo Total.
    [Fact]
    public async Task LevantarBloqueoTotalAsync_DespuesDeQueUnAdministradorApruebaLaSolicitud_LaTarjetaVuelveAActiva()
    {
        // Preparar
        var agente = await CrearUsuarioAsync(Usuario.CrearNuevo("agente", "agente@prueba.local", "hash", Ahora));
        var admin = await CrearUsuarioAsync(Usuario.CrearAdministradorInicial("admin", "admin@prueba.local", "hash", Ahora));
        var tarjeta = await TarjetaEnBloqueoTotalAsync(agente);
        var solicitud = await _permisos.CrearAsync(agente, CatalogoOperaciones.LevantarBloqueoTotal);
        var aprobacion = await _permisos.AprobarAsync(solicitud.Solicitud!.Id, admin, minutosVigencia: 30);
        Assert.Equal(EstadoResolucion.Resuelta, aprobacion.Estado);

        // Actuar
        var resultado = await _tarjetas.LevantarBloqueoTotalAsync(tarjeta, agente, Nota);

        // Verificar
        Assert.Equal(EstadoOperacionTarjeta.Exito, resultado.Estado);
        Assert.Equal(EstadoTarjeta.Activa, LeerEstado(tarjeta));
    }

    // Una solicitud rechazada tampoco da el permiso.
    [Fact]
    public async Task LevantarBloqueoTotalAsync_ConLaSolicitudRechazada_SeRechazaYLaTarjetaSigueEnBloqueoTotal()
    {
        // Preparar
        var agente = await CrearUsuarioAsync(Usuario.CrearNuevo("agente", "agente@prueba.local", "hash", Ahora));
        var admin = await CrearUsuarioAsync(Usuario.CrearAdministradorInicial("admin", "admin@prueba.local", "hash", Ahora));
        var tarjeta = await TarjetaEnBloqueoTotalAsync(agente);
        var solicitud = await _permisos.CrearAsync(agente, CatalogoOperaciones.LevantarBloqueoTotal);
        await _permisos.RechazarAsync(solicitud.Solicitud!.Id, admin, "Falta validar con el cliente.");

        // Actuar
        var resultado = await _tarjetas.LevantarBloqueoTotalAsync(tarjeta, agente, Nota);

        // Verificar
        Assert.Equal(EstadoOperacionTarjeta.SinPermiso, resultado.Estado);
        Assert.Equal(EstadoTarjeta.BloqueoTotal, LeerEstado(tarjeta));
    }

    // Emite una tarjeta y la lleva a BloqueoTotal por el camino real: una ráfaga de declinaciones
    // (BloqueoSuave, la pone el sistema) y luego el cliente no reconoce la actividad.
    private async Task<int> TarjetaEnBloqueoTotalAsync(int agente)
    {
        var emitida = await _tarjetas.EmitirAsync("Ana Pérez", "1234", 100m);
        var tarjeta = emitida.Tarjeta!.Id;
        for (var i = 0; i < ServicioTarjetas.DeclinacionesParaRafaga; i++)
            await _tarjetas.RegistrarTransaccionAsync(tarjeta, 500m, "Tienda en línea");
        Assert.Equal(EstadoTarjeta.BloqueoSuave, LeerEstado(tarjeta));

        var noReconoce = await _tarjetas.NoReconocerActividadAsync(tarjeta, agente, "El cliente no reconoce las compras.");
        Assert.Equal(EstadoOperacionTarjeta.Exito, noReconoce.Estado);
        return tarjeta;
    }

    private async Task<int> CrearUsuarioAsync(Usuario usuario)
    {
        _contexto.Usuarios.Add(usuario);
        await _contexto.SaveChangesAsync();
        return usuario.Id;
    }

    // Lee el estado guardado en la base, no el de la entidad en memoria.
    private EstadoTarjeta LeerEstado(int tarjetaId) =>
        _contexto.Set<Tarjeta>().AsNoTracking().Single(t => t.Id == tarjetaId).Estado;

    private sealed class RelojFijo(DateTime ahoraUtc) : IReloj
    {
        public DateTime AhoraUtc { get; } = ahoraUtc;
    }
}
