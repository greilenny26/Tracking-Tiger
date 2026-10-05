using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.ControlAcceso;

// RF-CA-05, RF-CA-06, RD-06: único punto que aplica la exigencia de acceso de cada operación.
// Lee la operación del endpoint ([Operacion]) y su exigencia en CatalogoOperaciones.
// - Operación sin [Operacion] o sin entrada en el catálogo → se rechaza (denegar por defecto).
// - Sin sesión válida → 401 del esquema de sesión (mismo mensaje de siempre).
// - Rol requerido → el rol se carga de la BASE DE DATOS en cada petición; nunca del cuerpo,
//   de los encabezados ni del token.
// La decisión depende SOLO de la sesión del servidor (usuario resuelto por el hash del token) y del rol
// guardado en la base: nunca de un rol o claim enviado por el cliente en el cuerpo, la query o encabezados.
public sealed class FiltroAutorizacionOperaciones : IAsyncAuthorizationFilter
{
    // Operación sin declarar en el catálogo (denegar por defecto): error de configuración, mensaje corto.
    private const string MensajeOperacionNoPermitida = "No tienes permiso para realizar esta operación.";

    private readonly ContextoDatos _contexto;
    private readonly ILogger<FiltroAutorizacionOperaciones> _registro;

    public FiltroAutorizacionOperaciones(ContextoDatos contexto, ILogger<FiltroAutorizacionOperaciones> registro)
    {
        _contexto = contexto;
        _registro = registro;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext contexto)
    {
        var operacion = contexto.ActionDescriptor.EndpointMetadata.OfType<OperacionAttribute>().FirstOrDefault();
        if (operacion is null || !CatalogoOperaciones.Exigencias.TryGetValue(operacion.Nombre, out var exigencia))
        {
            // Error de configuración: solo al log del servidor; al cliente, el mensaje corto.
            _registro.LogWarning("Endpoint sin operación declarada en el catálogo: {Accion}", contexto.ActionDescriptor.DisplayName);
            contexto.Result = Prohibido(MensajeOperacionNoPermitida);
            return;
        }

        if (exigencia.Tipo == TipoExigencia.Publica)
            return;

        // El esquema de sesión ya validó la credencial (sesión existente, no revocada, no vencida, usuario activo).
        if (contexto.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            contexto.Result = new ChallengeResult();
            return;
        }

        if (exigencia.Tipo == TipoExigencia.Autenticada)
            return;

        if (!int.TryParse(contexto.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
        {
            contexto.Result = new ChallengeResult();
            return;
        }

        var rol = await _contexto.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => (Rol?)u.Rol)
            .SingleOrDefaultAsync();

        if (rol is null)
            contexto.Result = new ChallengeResult();
        else if (rol != exigencia.RolRequerido)
        {
            // RF-CA-06: rechazo explícito. En el log solo el id del usuario y el nombre de la operación.
            _registro.LogWarning("Acceso denegado: usuario {UsuarioId} sin el rol requerido para la operación {Operacion}.",
                usuarioId, operacion.Nombre);
            contexto.Result = Prohibido(
                $"No tienes permiso para ejecutar esta operación. Se requiere el rol {exigencia.RolRequerido!.Value.NombreVisible()}.");
        }
    }

    private static ObjectResult Prohibido(string mensaje) =>
        new(new { mensaje }) { StatusCode = StatusCodes.Status403Forbidden };
}
