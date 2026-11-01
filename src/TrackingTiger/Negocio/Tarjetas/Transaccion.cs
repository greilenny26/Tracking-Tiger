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

    // Solo para EF Core al leer filas.
    private Transaccion()
    {
    }

    public static Transaccion Aprobar(int tarjetaId, decimal monto, string comercio, DateTime ahoraUtc) =>
        new() { TarjetaId = tarjetaId, Monto = monto, Comercio = comercio, Fecha = ahoraUtc, Aprobada = true };

    public static Transaccion Declinar(int tarjetaId, decimal monto, string comercio, string motivo, DateTime ahoraUtc) =>
        new()
        {
            TarjetaId = tarjetaId, Monto = monto, Comercio = comercio, Fecha = ahoraUtc, Aprobada = false,
            MotivoDeclinacion = motivo
        };
}
