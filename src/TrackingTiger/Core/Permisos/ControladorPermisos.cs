using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Tracking_Tiger.Core.ControlAcceso;

namespace Tracking_Tiger.Core.Permisos;

// Endpoints de Gestión de permisos. Solo traducen HTTP ↔ servicio (RD-02).
// Quién puede ejecutar cada acción se declara en CatalogoOperaciones (RF-CA-05, RD-06), nunca aquí.
[ApiController]
[Route("api/permisos/solicitudes")]
public sealed class ControladorPermisos : ControllerBase
{
    private readonly ServicioSolicitudesPermiso _servicio;

    public ControladorPermisos(ServicioSolicitudesPermiso servicio)
    {
        _servicio = servicio;
    }

    // RF-GP-01: 201 con la solicitud en Pendiente, 400 si el permiso no es solicitable,
    // 409 si ya hay una pendiente del mismo permiso.
    [Operacion(CatalogoOperaciones.CrearSolicitudPermiso)]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] SolicitudCrearPermiso cuerpo)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
            return Unauthorized(new { mensaje = "Sesión no válida o vencida." });

        var resultado = await _servicio.CrearAsync(usuarioId, cuerpo.Permiso);

        return resultado.Estado switch
        {
            EstadoCreacionSolicitud.Creada => StatusCode(StatusCodes.Status201Created, resultado.Solicitud),
            EstadoCreacionSolicitud.YaPendiente => Conflict(new { mensaje = resultado.Mensaje }),
            _ => BadRequest(new { mensaje = resultado.Mensaje })
        };
    }

    // RF-GP-02: solicitudes pendientes de todos los usuarios (solo Administrador).
    [Operacion(CatalogoOperaciones.ConsultarSolicitudesPendientes)]
    [HttpGet("pendientes")]
    public async Task<IActionResult> ListarPendientes() => Ok(await _servicio.ListarPendientesAsync());

    // RF-GP-02: las solicitudes del usuario de la sesión; nunca las de otros.
    [Operacion(CatalogoOperaciones.ConsultarMisSolicitudes)]
    [HttpGet("mias")]
    public async Task<IActionResult> ListarMias()
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
            return Unauthorized(new { mensaje = "Sesión no válida o vencida." });

        return Ok(await _servicio.ListarDeUsuarioAsync(usuarioId));
    }

    // El filtro ya garantizó una sesión válida; el id sale de esa sesión (claim del servidor).
    private bool TryObtenerUsuarioId(out int usuarioId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);
}

// Cuerpo JSON de POST /api/permisos/solicitudes. Admite null para dar un mensaje controlado (RD-07).
public sealed record SolicitudCrearPermiso(string? Permiso);
