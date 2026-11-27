using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.ControlAcceso;
using Tracking_Tiger.Core.Correo;
using Tracking_Tiger.Core.Notificaciones;
using Tracking_Tiger.Core.Permisos;
using Tracking_Tiger.Negocio.Tarjetas;
using TrackingTiger.Pruebas.Tarjetas;

namespace TrackingTiger.Pruebas.Notificaciones;

// Criterios N-01 a N-16 de docs/criterios-notificaciones-reportes.md. Sin levantar la aplicación (RD-12).
public sealed class NotificacionesPruebas : IDisposable
{
    private const string Permiso = CatalogoOperaciones.ListarUsuarios;

    private readonly BaseEnMemoria _base = new();
    private readonly ServicioNotificaciones _notificaciones;
    private readonly ServicioSolicitudesPermiso _permisos;
    private readonly ServicioTarjetas _tarjetas;

    public NotificacionesPruebas()
    {
        _notificaciones = new ServicioNotificaciones(_base.Contexto, _base.Reloj, new ColaCorreo(_base.Contexto, _base.Reloj));
        _permisos = new ServicioSolicitudesPermiso(_base.Contexto, _base.Reloj, _notificaciones);
        _tarjetas = new ServicioTarjetas(_base.Contexto, _base.Reloj, _permisos, new TasaCambioFalsa(), _notificaciones);
    }

    public void Dispose() => _base.Dispose();

    // N-01 (RF-NOT-01)
    [Fact]
    public async Task CrearSolicitud_SoloLosAdministradoresActivosRecibenLaNotificacion()
    {
        // Preparar
        var admin1 = await _base.CrearAdministradorAsync("admin1");
        var admin2 = await _base.CrearAdministradorAsync("admin2");
        var desactivado = await _base.CrearAdministradorAsync("admin3");
        await _base.DesactivarAsync(desactivado);
        var ana = await _base.CrearEstandarAsync("ana");
        await _base.CrearEstandarAsync("luis");

        // Actuar
        await _permisos.CrearAsync(ana, Permiso);

        // Verificar
        var destinatarios = await _base.Contexto.Notificaciones.Select(n => n.DestinatarioId).OrderBy(i => i).ToListAsync();
        Assert.Equal([admin1, admin2], destinatarios);
        Assert.All(await _base.Contexto.Notificaciones.ToListAsync(), n => Assert.False(n.Leida));
    }

    // N-02 (RF-NOT-01)
    [Fact]
    public async Task CrearSolicitud_Rechazada_NoNotificaNiEncola()
    {
        // Preparar
        await _base.CrearAdministradorAsync("admin");
        var ana = await _base.CrearEstandarAsync("ana");

        // Actuar
        await _permisos.CrearAsync(ana, "cambiar-rol");

        // Verificar
        Assert.Empty(await _base.Contexto.Notificaciones.ToListAsync());
        Assert.Empty(await _base.Contexto.CorreosEnCola.ToListAsync());
    }

    // N-03 y N-05 (RF-NOT-02)
    [Fact]
    public async Task Aprobar_NotificaSoloAlSolicitante_YRepetirNoNotificaOtraVez()
    {
        // Preparar
        var admin = await _base.CrearAdministradorAsync("admin");
        var ana = await _base.CrearEstandarAsync("ana");
        var solicitud = (await _permisos.CrearAsync(ana, Permiso)).Solicitud!.Id;

        // Actuar
        await _permisos.AprobarAsync(solicitud, admin, 30);
        await _permisos.AprobarAsync(solicitud, admin, 30);

        // Verificar
        var deAna = Assert.Single(await _notificaciones.ListarAsync(ana));
        Assert.Contains("aprobada", deAna.Mensaje);
        Assert.Equal(2, await _base.Contexto.Notificaciones.CountAsync());
    }

    // N-04 (RF-NOT-02)
    [Fact]
    public async Task Rechazar_NotificaAlSolicitanteConElMotivo()
    {
        // Preparar
        var admin = await _base.CrearAdministradorAsync("admin");
        var ana = await _base.CrearEstandarAsync("ana");
        var solicitud = (await _permisos.CrearAsync(ana, Permiso)).Solicitud!.Id;

        // Actuar
        await _permisos.RechazarAsync(solicitud, admin, "No lo necesitas.");

        // Verificar
        var deAna = Assert.Single(await _notificaciones.ListarAsync(ana));
        Assert.Contains("rechazada", deAna.Mensaje);
        Assert.Contains("No lo necesitas.", deAna.Mensaje);
    }

    // N-06, N-07 y N-07c (RF-NOT-03)
    [Fact]
    public async Task BloqueoSuave_AvisaUnaVezSoloAlAgenteQueTrabajoConLaTarjeta()
    {
        // Preparar
        var admin = await _base.CrearAdministradorAsync("admin");
        var ana = await _base.CrearEstandarAsync("ana");
        var luis = await _base.CrearEstandarAsync("luis");
        var tarjeta = await EmitirAsync();
        await RafagaAsync(tarjeta);
        await _tarjetas.ReconocerActividadAsync(tarjeta, ana);
        _base.Reloj.AhoraUtc = _base.Reloj.AhoraUtc.AddHours(1);
        await RafagaAsync(tarjeta);
        await _tarjetas.ReconocerActividadAsync(tarjeta, ana);
        var antes = (await _notificaciones.ListarAsync(ana)).Count;
        _base.Reloj.AhoraUtc = _base.Reloj.AhoraUtc.AddHours(1);

        // Actuar
        await RafagaAsync(tarjeta);
        await _tarjetas.RegistrarTransaccionAsync(tarjeta, 500m, "Tienda");

        // Verificar
        var avisos = await _notificaciones.ListarAsync(ana);
        Assert.Equal(antes + 1, avisos.Count);
        Assert.Contains($"tarjeta {tarjeta} (**** 4321)", avisos[0].Mensaje);
        Assert.Empty(await _notificaciones.ListarAsync(luis));
        Assert.Empty(await _notificaciones.ListarAsync(admin));
    }

    // N-06 (RF-NOT-03): el límite de 48 horas está incluido; N-07a: pasado el límite no se avisa.
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public async Task BloqueoSuave_RespetaElLimiteDe48Horas(int segundosDeMas, int esperadas)
    {
        // Preparar
        var ana = await _base.CrearEstandarAsync("ana");
        var tarjeta = await EmitirAsync();
        await RafagaAsync(tarjeta);
        await _tarjetas.ReconocerActividadAsync(tarjeta, ana);
        _base.Reloj.AhoraUtc = _base.Reloj.AhoraUtc.AddHours(48).AddSeconds(segundosDeMas);

        // Actuar
        await RafagaAsync(tarjeta);

        // Verificar
        Assert.Equal(esperadas, (await _notificaciones.ListarAsync(ana)).Count);
    }

    // N-07b (RF-NOT-03)
    [Fact]
    public async Task BloqueoSuave_SinAgentesRecientes_CambiaDeEstadoSinAvisos()
    {
        // Preparar
        await _base.CrearEstandarAsync("ana");
        var tarjeta = await EmitirAsync();

        // Actuar
        await RafagaAsync(tarjeta);

        // Verificar
        Assert.Equal("BloqueoSuave", (await _tarjetas.ConsultarAsync(tarjeta)).Tarjeta!.Estado);
        Assert.Empty(await _base.Contexto.Notificaciones.ToListAsync());
        Assert.Empty(await _base.Contexto.CorreosEnCola.ToListAsync());
    }

    // N-08 y N-09 (RF-NOT-04)
    [Fact]
    public async Task Listar_DistingueLeidas_YNoMuestraLasDeOtros()
    {
        // Preparar
        var ana = await _base.CrearEstandarAsync("ana");
        var luis = await _base.CrearEstandarAsync("luis");
        await NotificarAsync(ana, "primera");
        await NotificarAsync(ana, "segunda");
        await NotificarAsync(luis, "de luis");
        var primera = (await _notificaciones.ListarAsync(ana)).Single(n => n.Mensaje == "primera").Id;

        // Actuar
        await _notificaciones.MarcarLeidaAsync(primera, ana);

        // Verificar
        Assert.Equal(2, (await _notificaciones.ListarAsync(ana)).Count);
        Assert.Equal("segunda", Assert.Single(await _notificaciones.ListarAsync(ana, leida: false)).Mensaje);
        Assert.Equal("primera", Assert.Single(await _notificaciones.ListarAsync(ana, leida: true)).Mensaje);
    }

    // N-10, N-11 y N-12 (RF-NOT-05)
    [Fact]
    public async Task MarcarLeida_SoloElDestinatario()
    {
        // Preparar
        var admin = await _base.CrearAdministradorAsync("admin");
        var ana = await _base.CrearEstandarAsync("ana");
        await NotificarAsync(ana, "aviso");
        var id = (await _notificaciones.ListarAsync(ana)).Single().Id;

        // Actuar y verificar
        Assert.Equal(ResultadoMarcarLeida.Ajena, await _notificaciones.MarcarLeidaAsync(id, admin));
        Assert.False((await _notificaciones.ListarAsync(ana)).Single().Leida);
        Assert.Equal(ResultadoMarcarLeida.Marcada, await _notificaciones.MarcarLeidaAsync(id, ana));
        Assert.Equal(ResultadoMarcarLeida.Marcada, await _notificaciones.MarcarLeidaAsync(id, ana));
        Assert.True((await _notificaciones.ListarAsync(ana)).Single().Leida);
        Assert.Equal(ResultadoMarcarLeida.NoEncontrada, await _notificaciones.MarcarLeidaAsync(999, ana));
    }

    // N-14, N-15 y N-16 (RF-NOT-06, RF-NOT-07)
    [Fact]
    public async Task CorreoDesactivado_ConservaLaInternaSinEncolar()
    {
        // Preparar
        var conCorreo = await _base.CrearAdministradorAsync("admin1");
        var sinCorreo = await _base.CrearAdministradorAsync("admin2");
        var ana = await _base.CrearEstandarAsync("ana");
        await _notificaciones.CambiarCorreoAsync(sinCorreo, false);
        _base.Contexto.ChangeTracker.Clear();

        // Actuar
        await _permisos.CrearAsync(ana, Permiso);

        // Verificar
        Assert.False(await _notificaciones.ConsultarCorreoActivadoAsync(sinCorreo));
        Assert.Single(await _notificaciones.ListarAsync(conCorreo));
        Assert.Single(await _notificaciones.ListarAsync(sinCorreo));
        var correo = Assert.Single(await _base.Contexto.CorreosEnCola.ToListAsync());
        Assert.Equal("admin1@prueba.local", correo.Destinatario);
        Assert.Equal(EstadoCorreo.Pendiente, correo.Estado);
        Assert.Equal(0, correo.Intentos);
    }

    private async Task NotificarAsync(int destinatario, string mensaje)
    {
        await _notificaciones.AgregarAsync([destinatario], "prueba", "Asunto", mensaje);
        await _base.Contexto.SaveChangesAsync();
    }

    private async Task<int> EmitirAsync() => (await _tarjetas.EmitirAsync("Ana Pérez", "4321", 100m)).Tarjeta!.Id;

    private async Task RafagaAsync(int tarjeta)
    {
        for (var i = 0; i < ServicioTarjetas.DeclinacionesParaRafaga; i++)
            await _tarjetas.RegistrarTransaccionAsync(tarjeta, 500m, "Tienda");
    }
}
