using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Tracking_Tiger.Core.ControlAcceso;

namespace Tracking_Tiger.Core.Reportes;

// Endpoints de reportes del Core. Solo traducen HTTP ↔ servicio (RD-02).
[ApiController]
[Route("api/reportes")]
public sealed class ControladorReportes : ControllerBase
{
    private readonly ServicioReporteSolicitudes _servicio;

    public ControladorReportes(ServicioReporteSolicitudes servicio)
    {
        _servicio = servicio;
    }

    // RF-REP-01, 04, 05: 200 con los grupos; 400 si el rango no es válido.
    [Operacion(CatalogoOperaciones.ReporteSolicitudesPermiso)]
    [HttpGet("solicitudes-permiso")]
    public async Task<IActionResult> SolicitudesPermiso([FromQuery] string? desde, [FromQuery] string? hasta)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized(new { mensaje = "Sesión no válida o vencida." });

        var (rango, error) = RangoFechas.Leer(desde, hasta);
        return rango is null
            ? BadRequest(new { mensaje = error })
            : Ok(await _servicio.GenerarAsync(usuarioId, rango));
    }
}
