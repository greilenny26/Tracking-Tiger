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
public sealed class FiltroAutorizacionOperaciones : IAsyncAuthorizationFilter
{
    private const string MensajeSinPermiso = "No tienes permiso para realizar esta operación.";

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
            contexto.Result = Prohibido();
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
            contexto.Result = Prohibido();
    }

    private static ObjectResult Prohibido() =>
        new(new { mensaje = MensajeSinPermiso }) { StatusCode = StatusCodes.Status403Forbidden };
}
