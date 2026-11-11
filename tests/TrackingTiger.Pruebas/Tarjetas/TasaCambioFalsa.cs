using Tracking_Tiger.Negocio.Tarjetas;

namespace TrackingTiger.Pruebas.Tarjetas;

// Doble de la API de tasa de cambio: devuelve una tasa fija o, si se le da un error, lo lanza. No usa internet.
internal sealed class TasaCambioFalsa(decimal tasa = 60m, string? error = null) : IProveedorTasaCambio
{
    public int Consultas { get; private set; }

    public Task<decimal> ObtenerTasaAsync(string monedaOrigen, string monedaDestino, CancellationToken cancelacion = default)
    {
        Consultas++;
        return error is null ? Task.FromResult(tasa) : throw new ErrorTasaCambio(error);
    }
}
