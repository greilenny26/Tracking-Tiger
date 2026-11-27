using Tracking_Tiger.Core.ControlAcceso;
using Tracking_Tiger.Core.Permisos;
using Tracking_Tiger.Core.Reportes;
using Tracking_Tiger.Negocio.Tarjetas;

namespace TrackingTiger.Pruebas.Reportes;

// Criterios R-01 a R-10 de docs/criterios-notificaciones-reportes.md.
public sealed class ReportesPruebas : IDisposable
{
    private static readonly RangoFechas Todo = new(null, null);

    private readonly BaseEnMemoria _base = new();
    private readonly ServicioReporteSolicitudes _solicitudes;
    private readonly ServicioReporteTarjetas _comercios;

    public ReportesPruebas()
    {
        _solicitudes = new ServicioReporteSolicitudes(_base.Contexto);
        _comercios = new ServicioReporteTarjetas(_base.Contexto);
    }

    public void Dispose() => _base.Dispose();

    // R-01 y R-04 (RF-REP-01, 03, 04)
    [Fact]
    public async Task Solicitudes_AgrupaPorDiaYEstado_YElEstandarSoloVeLasSuyas()
    {
        // Preparar
        var admin = await _base.CrearAdministradorAsync("admin");
        var ana = await _base.CrearEstandarAsync("ana");
        var luis = await _base.CrearEstandarAsync("luis");
        var eva = await _base.CrearEstandarAsync("eva");
        var sara = await _base.CrearEstandarAsync("sara");
        await SolicitarAsync(ana);
        await SolicitarAsync(luis);
        await SolicitarAsync(eva);
        var rechazada = await SolicitarAsync(sara);
        await new ServicioSolicitudesPermiso(_base.Contexto, _base.Reloj).RechazarAsync(rechazada, admin, "No.");

        // Actuar
        var delAdministrador = await _solicitudes.GenerarAsync(admin, Todo);
        var deAna = await _solicitudes.GenerarAsync(ana, Todo);

        // Verificar
        Assert.Equal(
            [new FilaReporteSolicitudes("2026-11-20", "Pendiente", 3), new FilaReporteSolicitudes("2026-11-20", "Rechazada", 1)],
            delAdministrador);
        Assert.Equal([new FilaReporteSolicitudes("2026-11-20", "Pendiente", 1)], deAna);
    }

    // R-07, R-08 y R-10 (RF-REP-05) sobre el reporte del Core.
    [Fact]
    public async Task Solicitudes_FiltraPorRangoDeDias()
    {
        // Preparar
        var admin = await _base.CrearAdministradorAsync("admin");
        var ana = await _base.CrearEstandarAsync("ana");
        var luis = await _base.CrearEstandarAsync("luis");
        _base.Reloj.AhoraUtc = new DateTime(2026, 11, 20, 23, 59, 59, DateTimeKind.Utc);
        await SolicitarAsync(ana);
        _base.Reloj.AhoraUtc = new DateTime(2026, 11, 21, 0, 0, 0, DateTimeKind.Utc);
        await SolicitarAsync(luis);

        // Actuar y verificar
        Assert.Equal("2026-11-20", Assert.Single(await _solicitudes.GenerarAsync(admin, Rango("2026-11-20", "2026-11-20"))).Periodo);
        Assert.Equal("2026-11-21", Assert.Single(await _solicitudes.GenerarAsync(admin, Rango("2026-11-21", "2026-11-21"))).Periodo);
        Assert.Empty(await _solicitudes.GenerarAsync(admin, Rango("2026-01-01", "2026-01-31")));
    }

    // R-02 y R-03 (RF-REP-02, 03)
    [Fact]
    public async Task Comercios_SumaDeclinadasYOrdenaPorMontoDeclinado()
    {
        // Preparar
        var tarjeta = await EmitirAsync();
        await ComprarAsync(tarjeta, 50m, "A");
        await ComprarAsync(tarjeta, 2000m, "A");
        await ComprarAsync(tarjeta, 3000m, "A");
        await ComprarAsync(tarjeta, 9000m, "B");

        // Actuar
        var filas = await _comercios.RiesgoPorComercioAsync(Todo);

        // Verificar
        Assert.Equal(
            [new FilaRiesgoComercio("B", 1, 1, 100m, 9000m, 0m), new FilaRiesgoComercio("A", 3, 2, 66.67m, 5000m, 50m)],
            filas);
    }

    // R-07 y R-10 (RF-REP-05) sobre el reporte del negocio.
    [Fact]
    public async Task Comercios_FiltraPorRangoDeDias()
    {
        // Preparar
        var tarjeta = await EmitirAsync();
        await ComprarAsync(tarjeta, 10m, "A");
        _base.Reloj.AhoraUtc = _base.Reloj.AhoraUtc.AddDays(1);
        await ComprarAsync(tarjeta, 20m, "B");

        // Actuar y verificar
        Assert.Equal("A", Assert.Single(await _comercios.RiesgoPorComercioAsync(Rango("2026-11-20", "2026-11-20"))).Comercio);
        Assert.Equal("B", Assert.Single(await _comercios.RiesgoPorComercioAsync(Rango("2026-11-21", "2026-11-21"))).Comercio);
        Assert.Empty(await _comercios.RiesgoPorComercioAsync(Rango("2026-01-01", "2026-01-31")));
    }

    // R-09 (RD-07)
    [Theory]
    [InlineData("20-11-2026", null)]
    [InlineData(null, "2026-02-30")]
    [InlineData("2026-11-21", "2026-11-20")]
    public void Rango_NoValido_DevuelveMensaje(string? desde, string? hasta)
    {
        var (rango, error) = RangoFechas.Leer(desde, hasta);

        Assert.Null(rango);
        Assert.NotNull(error);
    }

    // R-05 (RF-REP-04): la exigencia de rol está declarada en el catálogo.
    [Fact]
    public void Catalogo_ElReporteDeComerciosYLaColaSonDelAdministrador()
    {
        var soloAdministrador = ExigenciaAcceso.DeRol(Rol.Administrador);

        Assert.Equal(soloAdministrador, CatalogoOperaciones.Exigencias[CatalogoOperaciones.ReporteRiesgoComercios]);
        Assert.Equal(soloAdministrador, CatalogoOperaciones.Exigencias[CatalogoOperaciones.ConsultarColaCorreos]);
    }

    private static RangoFechas Rango(string desde, string hasta) => RangoFechas.Leer(desde, hasta).Rango!;

    private async Task<int> SolicitarAsync(int usuario) =>
        (await new ServicioSolicitudesPermiso(_base.Contexto, _base.Reloj)
            .CrearAsync(usuario, CatalogoOperaciones.ListarUsuarios)).Solicitud!.Id;

    private ServicioTarjetas Tarjetas() => new(_base.Contexto, _base.Reloj,
        new ServicioSolicitudesPermiso(_base.Contexto, _base.Reloj), new Pruebas.Tarjetas.TasaCambioFalsa());

    private async Task<int> EmitirAsync() => (await Tarjetas().EmitirAsync("Ana", "1234", 100m)).Tarjeta!.Id;

    private Task ComprarAsync(int tarjeta, decimal monto, string comercio) =>
        Tarjetas().RegistrarTransaccionAsync(tarjeta, monto, comercio);
}
