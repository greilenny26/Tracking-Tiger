namespace Tracking_Tiger.Core.ControlAcceso;

// Arma el enlace de activación {APP_URL_BASE}/api/auth/activar?token={token} (RF-CA-15).
// APP_URL_BASE se lee al registrar, no al iniciar, así la API arranca aunque falte.
// Los mensajes de error nombran la variable, nunca el token ni el enlace.
public static class EnlaceActivacion
{
    public const string VariableUrlBase = "APP_URL_BASE";
    private const string Ruta = "/api/auth/activar";

    // leerVariable permite sustituir el origen de las variables; por defecto, el entorno del proceso.
    public static ResultadoEnlaceActivacion Construir(string tokenPlano, Func<string, string?>? leerVariable = null)
    {
        leerVariable ??= Environment.GetEnvironmentVariable;

        var urlBase = leerVariable(VariableUrlBase)?.Trim();
        if (string.IsNullOrWhiteSpace(urlBase))
            return ResultadoEnlaceActivacion.Error(
                $"Falta configurar la variable de entorno {VariableUrlBase} para armar el enlace de activación.");

        if (!Uri.TryCreate(urlBase, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            return ResultadoEnlaceActivacion.Error(
                $"La variable de entorno {VariableUrlBase} debe ser una URL http o https sin parámetros (por ejemplo, http://localhost:5000).");

        var enlace = $"{urlBase.TrimEnd('/')}{Ruta}?token={Uri.EscapeDataString(tokenPlano)}";
        return ResultadoEnlaceActivacion.Correcto(enlace);
    }
}

// Clase y no record: el ToString de un record imprimiría el enlace, que contiene el token.
public sealed class ResultadoEnlaceActivacion
{
    public bool Exito => Enlace is not null;
    public string? Enlace { get; }
    public string? Mensaje { get; }

    private ResultadoEnlaceActivacion(string? enlace, string? mensaje)
    {
        Enlace = enlace;
        Mensaje = mensaje;
    }

    public static ResultadoEnlaceActivacion Correcto(string enlace) => new(enlace, null);

    public static ResultadoEnlaceActivacion Error(string mensaje) => new(null, mensaje);
}
