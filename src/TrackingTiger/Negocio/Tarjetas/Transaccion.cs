namespace Tracking_Tiger.Negocio.Tarjetas;

// Un intento de compra con una tarjeta (simula lo que llega de la red de pago). Queda registrado
// tanto si se aprueba como si se declina; las declinaciones recientes alimentan la regla de ráfaga.
public class Transaccion
{
    public int Id { get; private set; }
    public int TarjetaId { get; private set; }
    public decimal Monto { get; private set; }
    public string Comercio { get; private set; } = string.Empty;
    public DateTime Fecha { get; private set; }
    public bool Aprobada { get; private set; }

    // Por qué se declinó; null si se aprobó.
    public string? MotivoDeclinacion { get; private set; }

    // Solo en compras en otra moneda: lo que se pagó y la tasa aplicada. En esos casos, Monto es el valor en DOP.
    public decimal? MontoOriginal { get; private set; }
    public string? MonedaOriginal { get; private set; }
    public decimal? TasaCambio { get; private set; }

    // Solo para EF Core al leer filas.
    private Transaccion()
    {
    }

    public static Transaccion Aprobar(int tarjetaId, decimal monto, string comercio, DateTime ahoraUtc,
        ConversionMoneda? conversion = null) =>
        new()
        {
            TarjetaId = tarjetaId, Monto = monto, Comercio = comercio, Fecha = ahoraUtc, Aprobada = true,
            MontoOriginal = conversion?.MontoOriginal, MonedaOriginal = conversion?.Moneda, TasaCambio = conversion?.Tasa
        };

    public static Transaccion Declinar(int tarjetaId, decimal monto, string comercio, string motivo, DateTime ahoraUtc,
        ConversionMoneda? conversion = null) =>
        new()
        {
            TarjetaId = tarjetaId, Monto = monto, Comercio = comercio, Fecha = ahoraUtc, Aprobada = false,
            MotivoDeclinacion = motivo,
            MontoOriginal = conversion?.MontoOriginal, MonedaOriginal = conversion?.Moneda, TasaCambio = conversion?.Tasa
        };
}

// Cómo se convirtió a DOP una compra hecha en otra moneda.
public sealed record ConversionMoneda(decimal MontoOriginal, string Moneda, decimal Tasa);
