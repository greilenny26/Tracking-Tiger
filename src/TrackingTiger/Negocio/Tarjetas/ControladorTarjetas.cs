using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Tracking_Tiger.Core.ControlAcceso;

namespace Tracking_Tiger.Negocio.Tarjetas;

// Endpoints del módulo Tarjetas. Solo traducen HTTP ↔ servicio (RD-02).
// Quién puede ejecutar cada acción se declara en CatalogoOperaciones (RF-NEG-06, RF-CA-05), nunca aquí.
[ApiController]
[Route("api/tarjetas")]
public sealed class ControladorTarjetas : ControllerBase
{
    private readonly ServicioTarjetas _servicio;

    public ControladorTarjetas(ServicioTarjetas servicio)
    {
        _servicio = servicio;
    }

    // 201 con la tarjeta en Activa; 400 si un dato no es válido.
    [Operacion(CatalogoOperaciones.EmitirTarjeta)]
    [HttpPost]
    public async Task<IActionResult> Emitir([FromBody] SolicitudEmitirTarjeta cuerpo)
    {
        var resultado = await _servicio.EmitirAsync(cuerpo.Titular, cuerpo.UltimosCuatro, cuerpo.LimiteCredito);
        return resultado.Estado == EstadoOperacionTarjeta.Exito
            ? StatusCode(StatusCodes.Status201Created, resultado.Tarjeta)
            : Responder(resultado);
    }

    // 200 con la transacción (aprobada o declinada) y la tarjeta; 400 si un dato no es válido; 404 si no existe;
    // 503 si la compra es en otra moneda y no se pudo obtener la tasa de cambio.
    [Operacion(CatalogoOperaciones.RegistrarTransaccion)]
    [HttpPost("{id:int}/transacciones")]
    public async Task<IActionResult> RegistrarTransaccion(int id, [FromBody] SolicitudRegistrarTransaccion cuerpo) =>
        Responder(await _servicio.RegistrarTransaccionAsync(id, cuerpo.Monto, cuerpo.Comercio, cuerpo.Moneda));

    [Operacion(CatalogoOperaciones.ConsultarTarjeta)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Consultar(int id) => Responder(await _servicio.ConsultarAsync(id));

    // Las cuatro acciones siguientes: 200 con la tarjeta en su nuevo estado; 400 si la máquina de estados no lo
    // permite (la tarjeta no cambia); 404 si no existe.
    [Operacion(CatalogoOperaciones.ReconocerActividad)]
    [HttpPost("{id:int}/reconocer")]
    public async Task<IActionResult> Reconocer(int id) =>
        TryObtenerUsuarioId(out var agenteId)
            ? Responder(await _servicio.ReconocerActividadAsync(id, agenteId))
            : SesionInvalida();

    [Operacion(CatalogoOperaciones.NoReconocerActividad)]
    [HttpPost("{id:int}/no-reconocer")]
    public async Task<IActionResult> NoReconocer(int id, [FromBody] SolicitudNotaTarjeta cuerpo) =>
        TryObtenerUsuarioId(out var agenteId)
            ? Responder(await _servicio.NoReconocerActividadAsync(id, agenteId, cuerpo.Nota))
            : SesionInvalida();

    // Además: 403 si el agente no tiene el permiso levantar-bloqueo-total aprobado y vigente.
    [Operacion(CatalogoOperaciones.LevantarBloqueoTotal)]
    [HttpPost("{id:int}/levantar-bloqueo-total")]
    public async Task<IActionResult> LevantarBloqueoTotal(int id, [FromBody] SolicitudNotaTarjeta cuerpo) =>
        TryObtenerUsuarioId(out var agenteId)
            ? Responder(await _servicio.LevantarBloqueoTotalAsync(id, agenteId, cuerpo.Nota))
            : SesionInvalida();

    [Operacion(CatalogoOperaciones.CancelarTarjeta)]
    [HttpPost("{id:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int id, [FromBody] SolicitudNotaTarjeta cuerpo) =>
        TryObtenerUsuarioId(out var agenteId)
            ? Responder(await _servicio.CancelarAsync(id, agenteId, cuerpo.Nota))
            : SesionInvalida();

    // 200 con las transiciones en orden; 404 si la tarjeta no existe.
    [Operacion(CatalogoOperaciones.ConsultarHistorialTarjeta)]
    [HttpGet("{id:int}/historial")]
    public async Task<IActionResult> Historial(int id)
    {
        var historial = await _servicio.ConsultarHistorialAsync(id);
        return historial is null ? NotFound(new { mensaje = "No existe una tarjeta con ese id." }) : Ok(historial);
    }

    private IActionResult Responder(ResultadoOperacionTarjeta resultado) => resultado.Estado switch
    {
        EstadoOperacionTarjeta.Exito => Ok(new { mensaje = resultado.Mensaje, tarjeta = resultado.Tarjeta, transaccion = resultado.Transaccion }),
        EstadoOperacionTarjeta.NoEncontrada => NotFound(new { mensaje = resultado.Mensaje }),
        EstadoOperacionTarjeta.SinPermiso => StatusCode(StatusCodes.Status403Forbidden, new { mensaje = resultado.Mensaje }),
        EstadoOperacionTarjeta.ServicioNoDisponible => StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensaje = resultado.Mensaje }),
        _ => BadRequest(new { mensaje = resultado.Mensaje })
    };

    private UnauthorizedObjectResult SesionInvalida() => Unauthorized(new { mensaje = "Sesión no válida o vencida." });

    // El filtro ya garantizó una sesión válida; el id sale de esa sesión (claim del servidor).
    private bool TryObtenerUsuarioId(out int usuarioId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);
}

// Cuerpos JSON. Admiten null para dar un mensaje controlado (RD-07).
public sealed record SolicitudEmitirTarjeta(string? Titular, string? UltimosCuatro, decimal? LimiteCredito);

public sealed record SolicitudRegistrarTransaccion(decimal? Monto, string? Comercio, string? Moneda);

public sealed record SolicitudNotaTarjeta(string? Nota);
