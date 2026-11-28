using Microsoft.AspNetCore.Mvc;
using Tracking_Tiger.Core.ControlAcceso;

namespace Tracking_Tiger.Core.Auditoria;

// Endpoint de auditoría. Solo traduce HTTP ↔ servicio (RD-02). Solo tiene GET: no existe ninguna
// operación para editar ni borrar registros (RF-AUD-06). Solo el Administrador (CatalogoOperaciones).
[ApiController]
[Route("api/auditoria")]
public sealed class ControladorAuditoria : ControllerBase
{
    private readonly ServicioConsultaAuditoria _servicio;

    public ControladorAuditoria(ServicioConsultaAuditoria servicio)
    {
        _servicio = servicio;
    }

    // RF-AUD-07: 200 con los registros, del más reciente al más antiguo; 400 si un filtro no es válido.
    [Operacion(CatalogoOperaciones.ConsultarAuditoria)]
    [HttpGet]
    public async Task<IActionResult> Consultar([FromQuery] string? usuarioId, [FromQuery] string? entidad,
        [FromQuery] string? desde, [FromQuery] string? hasta)
    {
        var (registros, error) = await _servicio.ConsultarAsync(usuarioId, entidad, desde, hasta);
        return registros is null ? BadRequest(new { mensaje = error }) : Ok(registros);
    }
}
