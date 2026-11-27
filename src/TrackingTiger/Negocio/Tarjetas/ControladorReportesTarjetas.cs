using Microsoft.AspNetCore.Mvc;
using Tracking_Tiger.Core.ControlAcceso;
using Tracking_Tiger.Core.Reportes;

namespace Tracking_Tiger.Negocio.Tarjetas;

// Reporte del módulo Tarjetas. RF-REP-04: resume transacciones de todas las tarjetas, así que solo
// el Administrador puede pedirlo (ver CatalogoOperaciones).
[ApiController]
[Route("api/reportes/tarjetas")]
public sealed class ControladorReportesTarjetas : ControllerBase
{
    private readonly ServicioReporteTarjetas _servicio;

    public ControladorReportesTarjetas(ServicioReporteTarjetas servicio)
    {
        _servicio = servicio;
    }

    [Operacion(CatalogoOperaciones.ReporteRiesgoComercios)]
    [HttpGet("riesgo-por-comercio")]
    public async Task<IActionResult> RiesgoPorComercio([FromQuery] string? desde, [FromQuery] string? hasta)
    {
        var (rango, error) = RangoFechas.Leer(desde, hasta);
        return rango is null
            ? BadRequest(new { mensaje = error })
            : Ok(await _servicio.RiesgoPorComercioAsync(rango));
    }
}
