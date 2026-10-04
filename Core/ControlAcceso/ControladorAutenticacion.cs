using Microsoft.AspNetCore.Mvc;

namespace Tracking_Tiger.Core.ControlAcceso;

// Endpoints de autenticación. Solo traducen HTTP ↔ servicio; las reglas viven en los servicios (RD-02).
[ApiController]
[Route("api/auth")]
public sealed class ControladorAutenticacion : ControllerBase
{
    private readonly ServicioRegistro _servicioRegistro;

    public ControladorAutenticacion(ServicioRegistro servicioRegistro)
    {
        _servicioRegistro = servicioRegistro;
    }

    // RF-CA-01: 201 con los datos públicos del usuario, 400 si los datos no son válidos,
    // 409 si el correo ya está registrado. La respuesta nunca incluye el hash.
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
                correo = resultado.Usuario.Correo
            }),
            EstadoRegistro.CorreoDuplicado => Conflict(new { mensaje = resultado.Mensaje }),
            _ => BadRequest(new { mensaje = resultado.Mensaje })
        };
    }
}
