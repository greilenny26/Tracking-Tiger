using System.Net;
using System.Text;
using Tracking_Tiger.Negocio.Tarjetas;

namespace TrackingTiger.Pruebas.Tarjetas;

// Conexión con ExchangeRate-API usando un HttpMessageHandler falso: no sale a internet.
public sealed class ProveedorTasaExchangeRatePruebas
{
    private const string ClaveDePrueba = "clave-solo-para-pruebas";
    private const string RespuestaCorrecta = """{"result":"success","base_code":"USD","conversion_rates":{"DOP":59.5,"USD":1}}""";

    private sealed class ManejadorFalso(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? Ultima { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage solicitud, CancellationToken cancelacion)
        {
            Ultima = solicitud;
            return Task.FromResult(responder(solicitud));
        }
    }

    private static HttpResponseMessage Json(HttpStatusCode codigo, string cuerpo) =>
        new(codigo) { Content = new StringContent(cuerpo, Encoding.UTF8, "application/json") };

    private static ProveedorTasaExchangeRate CrearProveedor(ManejadorFalso manejador, string? clave = ClaveDePrueba) =>
        new(new HttpClient(manejador), _ => clave);

    [Fact]
    public async Task ObtenerTasaAsync_ConRespuestaCorrecta_DevuelveLaTasaDeLaMonedaDestino()
    {
        // Preparar
        var proveedor = CrearProveedor(new ManejadorFalso(_ => Json(HttpStatusCode.OK, RespuestaCorrecta)));

        // Actuar
        var tasa = await proveedor.ObtenerTasaAsync("USD", "DOP");

        // Verificar
        Assert.Equal(59.5m, tasa);
    }

    // RD-10: la clave viaja en la cabecera; la URL, que sí puede acabar en un log, no la contiene.
    [Fact]
    public async Task ObtenerTasaAsync_EnviaLaClaveEnLaCabeceraYNuncaEnLaUrl()
    {
        // Preparar
        var manejador = new ManejadorFalso(_ => Json(HttpStatusCode.OK, RespuestaCorrecta));
        var proveedor = CrearProveedor(manejador);

        // Actuar
        await proveedor.ObtenerTasaAsync("USD", "DOP");

        // Verificar
        Assert.Equal("Bearer", manejador.Ultima!.Headers.Authorization!.Scheme);
        Assert.Equal(ClaveDePrueba, manejador.Ultima.Headers.Authorization.Parameter);
        Assert.DoesNotContain(ClaveDePrueba, manejador.Ultima.RequestUri!.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task ObtenerTasaAsync_SinClave_RechazaSinLlamarALaApiYNombraLaVariable(string? clave)
    {
        // Preparar
        var manejador = new ManejadorFalso(_ => Json(HttpStatusCode.OK, RespuestaCorrecta));
        var proveedor = CrearProveedor(manejador, clave);

        // Actuar
        var error = await Assert.ThrowsAsync<ErrorTasaCambio>(() => proveedor.ObtenerTasaAsync("USD", "DOP"));

        // Verificar
        Assert.Contains(ProveedorTasaExchangeRate.VariableClave, error.Message);
        Assert.Null(manejador.Ultima);
    }

    // RD-08: clave rechazada, cuota agotada o respuesta rara: siempre el mismo mensaje, sin detalles ni la clave.
    [Theory]
    [InlineData(HttpStatusCode.Forbidden, """{"result":"error","error-type":"invalid-key"}""")]
    [InlineData(HttpStatusCode.TooManyRequests, """{"result":"error","error-type":"quota-reached"}""")]
    [InlineData(HttpStatusCode.OK, """{"result":"error","error-type":"unsupported-code"}""")]
    [InlineData(HttpStatusCode.OK, """{"result":"success","conversion_rates":{"USD":1}}""")]
    [InlineData(HttpStatusCode.OK, "esto no es JSON")]
    public async Task ObtenerTasaAsync_ConRespuestaInvalida_LanzaErrorControlado(HttpStatusCode codigo, string cuerpo)
    {
        // Preparar
        var proveedor = CrearProveedor(new ManejadorFalso(_ => Json(codigo, cuerpo)));

        // Actuar
        var error = await Assert.ThrowsAsync<ErrorTasaCambio>(() => proveedor.ObtenerTasaAsync("USD", "DOP"));

        // Verificar
        Assert.Contains("no está disponible", error.Message);
        Assert.DoesNotContain(ClaveDePrueba, error.Message);
    }

    [Fact]
    public async Task ObtenerTasaAsync_ConLaApiCaida_LanzaErrorControlado()
    {
        // Preparar
        var proveedor = CrearProveedor(new ManejadorFalso(_ => throw new HttpRequestException("detalle interno de red")));

        // Actuar
        var error = await Assert.ThrowsAsync<ErrorTasaCambio>(() => proveedor.ObtenerTasaAsync("USD", "DOP"));

        // Verificar
        Assert.DoesNotContain("detalle interno", error.Message);
    }
}
