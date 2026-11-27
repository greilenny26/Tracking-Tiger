using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Tracking_Tiger.Core.ControlAcceso;

namespace Tracking_Tiger.Core.Notificaciones;

// Endpoints de Notificaciones. Solo traducen HTTP ↔ servicio (RD-02).
// Quién puede ejecutar cada acción se declara en CatalogoOperaciones (RF-CA-05, RD-06), nunca aquí.
[ApiController]
[Route("api/notificaciones")]
public sealed class ControladorNotificaciones : ControllerBase
{
    private readonly ServicioNotificaciones _servicio;

    public ControladorNotificaciones(ServicioNotificaciones servicio)
    {
        _servicio = servicio;
    }

    // RF-NOT-04: las del usuario de la sesión. ?leida=true o false filtra.
    [Operacion(CatalogoOperaciones.ConsultarNotificaciones)]
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool? leida) =>
        TryObtenerUsuarioId(out var usuarioId)
            ? Ok(await _servicio.ListarAsync(usuarioId, leida))
            : SesionInvalida();

    // RF-NOT-05: 200 si es propia; 403 si es de otro usuario (no cambia); 404 si no existe.
    [Operacion(CatalogoOperaciones.MarcarNotificacionLeida)]
    [HttpPost("{id:int}/leer")]
    public async Task<IActionResult> MarcarLeida(int id)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
            return SesionInvalida();

        return await _servicio.MarcarLeidaAsync(id, usuarioId) switch
        {
            ResultadoMarcarLeida.Marcada => Ok(new { mensaje = "Notificación marcada como leída." }),
            ResultadoMarcarLeida.Ajena => StatusCode(StatusCodes.Status403Forbidden,
                new { mensaje = "Solo el destinatario puede marcar esta notificación." }),
            _ => NotFound(new { mensaje = "No existe una notificación con ese id." })
        };
    }

    [Operacion(CatalogoOperaciones.ConsultarPreferenciasNotificacion)]
    [HttpGet("preferencias")]
    public async Task<IActionResult> ConsultarPreferencias() =>
        TryObtenerUsuarioId(out var usuarioId)
            ? Ok(new { correo = await _servicio.ConsultarCorreoActivadoAsync(usuarioId) })
            : SesionInvalida();

    // RF-NOT-07: {"correo": false} desactiva el correo sin perder el canal interno.
    [Operacion(CatalogoOperaciones.CambiarPreferenciasNotificacion)]
    [HttpPut("preferencias")]
    public async Task<IActionResult> CambiarPreferencias([FromBody] SolicitudPreferencias cuerpo)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
            return SesionInvalida();
        if (cuerpo.Correo is null)
            return BadRequest(new { mensaje = "El campo correo es obligatorio: true o false." });

        await _servicio.CambiarCorreoAsync(usuarioId, cuerpo.Correo.Value);
        return Ok(new { correo = cuerpo.Correo.Value });
    }

    private UnauthorizedObjectResult SesionInvalida() => Unauthorized(new { mensaje = "Sesión no válida o vencida." });

    private bool TryObtenerUsuarioId(out int usuarioId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);
}

// Cuerpo JSON de PUT /api/notificaciones/preferencias. Admite null para dar un mensaje controlado (RD-07).
public sealed record SolicitudPreferencias(bool? Correo);
