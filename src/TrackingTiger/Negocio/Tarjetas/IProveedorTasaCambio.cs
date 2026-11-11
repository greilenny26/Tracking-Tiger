namespace Tracking_Tiger.Negocio.Tarjetas;

// Origen de las tasas de cambio. Separarlo permite probar las compras en otra moneda sin llamar a internet.
public interface IProveedorTasaCambio
{
    // Cuántas unidades de monedaDestino vale una unidad de monedaOrigen.
    // Lanza ErrorTasaCambio si no hay clave configurada o si la API no responde bien.
    Task<decimal> ObtenerTasaAsync(string monedaOrigen, string monedaDestino, CancellationToken cancelacion = default);
}

// Falla controlada al consultar la tasa. Su mensaje ya es apto para mostrarse al usuario (RD-08).
public sealed class ErrorTasaCambio(string mensaje) : Exception(mensaje);
