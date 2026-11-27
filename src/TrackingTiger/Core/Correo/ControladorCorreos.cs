using Microsoft.AspNetCore.Mvc;
using Tracking_Tiger.Core.ControlAcceso;

namespace Tracking_Tiger.Core.Correo;

// Endpoint de la cola de correos (RF-NOT-11). Solo el Administrador (ver CatalogoOperaciones).
[ApiController]
[Route("api/correos")]
public sealed class ControladorCorreos : ControllerBase
{
    private readonly ServicioConsultaCorreos _servicio;

    public ControladorCorreos(ServicioConsultaCorreos servicio)
    {
        _servicio = servicio;
    }

    // ?estado=Pendiente, Enviado o Fallido filtra; otro valor responde 400 (RD-07).
    [Operacion(CatalogoOperaciones.ConsultarColaCorreos)]
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? estado)
    {
        if (string.IsNullOrWhiteSpace(estado))
            return Ok(await _servicio.ListarAsync());

        // Se compara por nombre: Enum.TryParse también aceptaría números.
        var nombre = Enum.GetNames<EstadoCorreo>()
            .FirstOrDefault(n => n.Equals(estado.Trim(), StringComparison.OrdinalIgnoreCase));
        if (nombre is null)
            return BadRequest(new { mensaje = $"estado debe ser uno de estos: {string.Join(", ", Enum.GetNames<EstadoCorreo>())}." });

        return Ok(await _servicio.ListarAsync(Enum.Parse<EstadoCorreo>(nombre)));
    }
}
