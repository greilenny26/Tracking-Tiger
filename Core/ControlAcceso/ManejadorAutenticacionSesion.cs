using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.ControlAcceso;

// Autenticación por credencial de sesión opaca (RF-CA-03, RF-CA-07): "Authorization: Bearer <token>".
// Único punto que valida sesiones; todo endpoint con [Authorize] lo reutiliza por ser el esquema por defecto.
// Se busca la sesión por el SHA-256 del token (en la base nunca está el token en claro).
// Con una entrada mal formada nunca lanza: responde "sin autenticar" y el cliente recibe 401.
public sealed class ManejadorAutenticacionSesion : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Esquema = "SesionBearer";
    public const string ClaimSesionId = "sesion_id";

    private const string Prefijo = "Bearer ";
    // Un token válido mide 43 caracteres; cualquier cosa mucho más larga se descarta sin procesar.
    private const int LargoMaximoToken = 128;

    private readonly ContextoDatos _contexto;

    public ManejadorAutenticacionSesion(IOptionsMonitor<AuthenticationSchemeOptions> opciones,
        ILoggerFactory registro, UrlEncoder codificador, ContextoDatos contexto)
        : base(opciones, registro, codificador)
    {
        _contexto = contexto;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Sin encabezado, o con otro esquema: no hay credencial que evaluar.
        var encabezado = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(encabezado) || !encabezado.StartsWith(Prefijo, StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        // Los mensajes de Fail solo van al log del servidor y nunca incluyen el token.
        var token = encabezado[Prefijo.Length..].Trim();
        if (token.Length == 0 || token.Length > LargoMaximoToken)
            return AuthenticateResult.Fail("Credencial de sesión mal formada.");

        var tokenHash = GeneradorTokens.CalcularHash(token);
        var sesion = await _contexto.SesionesUsuario
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.TokenHash == tokenHash);

        if (sesion is null || sesion.Revocada)
            return AuthenticateResult.Fail("Sesión no válida.");

        var identidad = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, sesion.UsuarioId.ToString()),
            new Claim(ClaimSesionId, sesion.Id.ToString())
        ], Esquema);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identidad), Esquema));
    }

    // 401 con mensaje en español en JSON (RD-08), en lugar de la respuesta vacía por defecto.
    protected override async Task HandleChallengeAsync(AuthenticationProperties propiedades)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        Response.Headers.WWWAuthenticate = "Bearer";
        await Response.WriteAsJsonAsync(new { mensaje = "Sesión no válida o vencida." });
    }
}
