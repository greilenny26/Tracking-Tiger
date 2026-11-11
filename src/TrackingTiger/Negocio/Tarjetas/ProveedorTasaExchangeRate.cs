using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tracking_Tiger.Negocio.Tarjetas;

// Implementación de IProveedorTasaCambio con ExchangeRate-API. Es el único lugar del proyecto que habla con esa API.
// La clave se envía en la cabecera Authorization y no en la URL: el registro de HttpClient escribe la URL en los logs.
// Toda falla se traduce a ErrorTasaCambio con un mensaje fijo: nunca se muestra el error original (RD-08).
public sealed class ProveedorTasaExchangeRate : IProveedorTasaCambio
{
    public const string VariableClave = "TASA_CAMBIO_API_KEY";

    // Límite para cada consulta, así una compra nunca se cuelga si la API no responde.
    public static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(10);

    private const string UrlBase = "https://v6.exchangerate-api.com/v6/latest/";
    private const string ResultadoCorrecto = "success";
    private const string MensajeNoDisponible = "El servicio de tasa de cambio no está disponible. Las compras en DOP siguen funcionando; intenta de nuevo más tarde.";

    private readonly HttpClient _http;
    private readonly Func<string, string?> _leerVariable;

    // leerVariable permite sustituir el origen de las variables; por defecto, el entorno del proceso.
    public ProveedorTasaExchangeRate(HttpClient http, Func<string, string?>? leerVariable = null)
    {
        _http = http;
        _leerVariable = leerVariable ?? Environment.GetEnvironmentVariable;
    }

    public async Task<decimal> ObtenerTasaAsync(string monedaOrigen, string monedaDestino, CancellationToken cancelacion = default)
    {
        // La clave se lee al usarla, no al iniciar: así la aplicación arranca aunque no esté configurada.
        var clave = _leerVariable(VariableClave)?.Trim();
        if (string.IsNullOrWhiteSpace(clave))
            throw new ErrorTasaCambio(
                $"La conversión de moneda no está configurada: falta la variable de entorno {VariableClave}. Las compras en DOP siguen funcionando.");

        try
        {
            using var solicitud = new HttpRequestMessage(HttpMethod.Get, UrlBase + monedaOrigen);
            solicitud.Headers.Authorization = new AuthenticationHeaderValue("Bearer", clave);

            using var respuesta = await _http.SendAsync(solicitud, cancelacion);
            if (!respuesta.IsSuccessStatusCode)
                throw new ErrorTasaCambio(MensajeNoDisponible);

            var datos = await respuesta.Content.ReadFromJsonAsync<RespuestaTasas>(cancelacion);
            if (datos?.Resultado != ResultadoCorrecto
                || datos.Tasas is null
                || !datos.Tasas.TryGetValue(monedaDestino, out var tasa)
                || tasa <= 0)
                throw new ErrorTasaCambio(MensajeNoDisponible);

            return tasa;
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or JsonException or NotSupportedException or FormatException)
        {
            throw new ErrorTasaCambio(MensajeNoDisponible);
        }
    }

    // Solo los campos que se usan de la respuesta de la API.
    private sealed record RespuestaTasas(
        [property: JsonPropertyName("result")] string? Resultado,
        [property: JsonPropertyName("conversion_rates")] Dictionary<string, decimal>? Tasas);
}
