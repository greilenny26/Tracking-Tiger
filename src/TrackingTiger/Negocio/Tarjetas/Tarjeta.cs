namespace Tracking_Tiger.Negocio.Tarjetas;

// Entidad central del módulo de negocio: una tarjeta de crédito vigilada por el sistema antifraude.
// Su estado solo cambia a través de MaquinaEstadosTarjeta (RD-04). Todas las fechas están en UTC (RD-11).
public class Tarjeta
{
    public int Id { get; private set; }
    public string Titular { get; private set; } = string.Empty;

    // Solo los últimos 4 dígitos (nunca el número completo).
    public string UltimosCuatro { get; private set; } = string.Empty;
    public decimal LimiteCredito { get; private set; }
    public decimal Consumido { get; private set; }
    public EstadoTarjeta Estado { get; private set; } = EstadoTarjeta.Activa;
    public DateTime FechaEmision { get; private set; }

    public decimal Disponible => LimiteCredito - Consumido;
    public string NumeroEnmascarado => $"**** {UltimosCuatro}";

    // Solo para EF Core al leer filas. Toda tarjeta nueva se crea con Emitir.
    private Tarjeta()
    {
    }

    // Toda tarjeta nace Activa y sin consumo. El servicio valida los datos antes de llamar aquí (RD-07).
    public static Tarjeta Emitir(string titular, string ultimosCuatro, decimal limiteCredito, DateTime ahoraUtc) =>
        new()
        {
            Titular = titular,
            UltimosCuatro = ultimosCuatro,
            LimiteCredito = limiteCredito,
            Consumido = 0,
            Estado = EstadoTarjeta.Activa,
            FechaEmision = ahoraUtc
        };

    // Una transacción aprobada consume crédito. El servicio ya comprobó que hay disponible.
    internal void RegistrarConsumo(decimal monto) => Consumido += monto;

    // Solo la invoca la máquina de estados después de validar la transición (RD-04).
    internal void AplicarEstado(EstadoTarjeta nuevoEstado) => Estado = nuevoEstado;
}
