using System.Net;
using System.Net.Http.Headers;

namespace Tracking_Tiger.Core.Documentos;

// Implementación de IAlmacenDocumentos con el Filer de SeaweedFS (API HTTP). Es el único lugar del proyecto que
// habla con SeaweedFS. En la composición, SeaweedFS guarda los archivos en un volumen con nombre (RF-DOC-05).
// Subir:    POST multipart a {SEAWEEDFS_URL}/{ruta}
// Descargar: GET {SEAWEEDFS_URL}/{ruta} (404 si no existe)
// Toda falla de red se traduce a ErrorAlmacenDocumentos con un mensaje fijo: nunca se muestra el error original.
public sealed class AlmacenSeaweedFs : IAlmacenDocumentos
{
    public const string VariableUrl = "SEAWEEDFS_URL";

    // Límite para cada operación, así una petición nunca se cuelga si SeaweedFS no responde.
    public static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(30);

    private const string MensajeNoDisponible = "El almacén de documentos no está disponible. Intenta de nuevo más tarde.";

    private readonly HttpClient _http;
    private readonly Func<string, string?> _leerVariable;

    // leerVariable permite sustituir el origen de las variables; por defecto, el entorno del proceso.
    public AlmacenSeaweedFs(HttpClient http, Func<string, string?>? leerVariable = null)
    {
        _http = http;
        _leerVariable = leerVariable ?? Environment.GetEnvironmentVariable;
    }

    public async Task GuardarAsync(string ruta, Stream contenido, string tipo, CancellationToken cancelacion = default)
    {
        var url = ArmarUrl(ruta);

        using var archivo = new StreamContent(contenido);
        archivo.Headers.ContentType = MediaTypeHeaderValue.TryParse(tipo, out var tipoContenido)
            ? tipoContenido
            : new MediaTypeHeaderValue("application/octet-stream");
        using var formulario = new MultipartFormDataContent { { archivo, "file", Path.GetFileName(ruta) } };

        try
        {
            using var respuesta = await _http.PostAsync(url, formulario, cancelacion);
            if (!respuesta.IsSuccessStatusCode)
                throw new ErrorAlmacenDocumentos(MensajeNoDisponible);
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            throw new ErrorAlmacenDocumentos(MensajeNoDisponible);
        }
    }

    public async Task<Stream?> AbrirAsync(string ruta, CancellationToken cancelacion = default)
    {
        var url = ArmarUrl(ruta);

        HttpResponseMessage respuesta;
        try
        {
            // ResponseHeadersRead: el archivo se reenvía al cliente por partes, sin cargarlo entero en memoria.
            respuesta = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancelacion);
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            throw new ErrorAlmacenDocumentos(MensajeNoDisponible);
        }

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            respuesta.Dispose();
            return null;
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            respuesta.Dispose();
            throw new ErrorAlmacenDocumentos(MensajeNoDisponible);
        }

        // Cerrar este stream libera también la respuesta.
        return await respuesta.Content.ReadAsStreamAsync(cancelacion);
    }

    // La URL se lee al usarla, no al iniciar: así la API arranca aunque SeaweedFS no esté configurado.
    private Uri ArmarUrl(string ruta)
    {
        var baseUrl = _leerVariable(VariableUrl)?.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl)
            || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
            || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            throw new ErrorAlmacenDocumentos(
                $"El almacén de documentos no está configurado: falta la variable de entorno {VariableUrl} (por ejemplo, http://seaweedfs:8888).");

        return new Uri($"{baseUrl}/{ruta}");
    }
}
