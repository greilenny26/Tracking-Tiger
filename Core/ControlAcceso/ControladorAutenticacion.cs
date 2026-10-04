using Microsoft.AspNetCore.Mvc;

namespace Tracking_Tiger.Core.ControlAcceso;

// Endpoints de autenticación. Solo traducen HTTP ↔ servicio; las reglas viven en los servicios (RD-02).
[ApiController]
[Route("api/auth")]
public sealed class ControladorAutenticacion : ControllerBase
{
    private readonly ServicioRegistro _servicioRegistro;
    private readonly ServicioActivacion _servicioActivacion;

    public ControladorAutenticacion(ServicioRegistro servicioRegistro, ServicioActivacion servicioActivacion)
    {
        _servicioRegistro = servicioRegistro;
        _servicioActivacion = servicioActivacion;
    }

    // RF-CA-01: 201 con los datos públicos del usuario, 400 si los datos no son válidos,
    // 409 si el correo ya está registrado, 503 si falta configuración del servidor.
    // La respuesta nunca incluye el hash ni el token de activación.
    [HttpPost("registro")]
    public async Task<IActionResult> Registrar([FromBody] SolicitudRegistro solicitud)
    {
        var resultado = await _servicioRegistro.RegistrarAsync(solicitud.Nombre, solicitud.Correo, solicitud.Contrasena);

        return resultado.Estado switch
        {
            EstadoRegistro.Registrado => StatusCode(StatusCodes.Status201Created, new
            {
                mensaje = resultado.Mensaje,
                id = resultado.Usuario!.Id,
                nombre = resultado.Usuario.Nombre,
                correo = resultado.Usuario.Correo,
                activo = resultado.Usuario.Activo
            }),
            EstadoRegistro.CorreoDuplicado => Conflict(new { mensaje = resultado.Mensaje }),
            EstadoRegistro.NoDisponible => StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensaje = resultado.Mensaje }),
            _ => BadRequest(new { mensaje = resultado.Mensaje })
        };
    }

    // RF-CA-16: 200 si la cuenta se activó, 400 con mensaje genérico si el enlace no es válido.
    [HttpGet("activar")]
    public async Task<IActionResult> Activar([FromQuery] string? token)
    {
        var resultado = await _servicioActivacion.ActivarAsync(token);

        return resultado.Estado == EstadoActivacion.Activada
            ? Ok(new { mensaje = resultado.Mensaje })
            : BadRequest(new { mensaje = resultado.Mensaje });
    }
}
