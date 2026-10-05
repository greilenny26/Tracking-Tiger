namespace Tracking_Tiger.Negocio.Alertas;

// Entidad central del módulo de negocio: una alerta sobre una transacción de tarjeta sospechosa de fraude.
// Su estado solo cambia a través de MaquinaEstadosAlerta (RD-04). Todas las fechas están en UTC.
public class AlertaFraude
{
    public int Id { get; private set; }

    // Solo los últimos 4 dígitos de la tarjeta (nunca el número completo), por ejemplo "**** 1234".
    public string NumeroTarjetaEnmascarado { get; private set; } = string.Empty;
    public decimal Monto { get; private set; }
    public string Comercio { get; private set; } = string.Empty;
    public DateTime FechaTransaccion { get; private set; }

    // Nivel de riesgo calculado por la detección, de 0 a 100.
    public int NivelRiesgo { get; private set; }

    public EstadoAlerta Estado { get; private set; } = EstadoAlerta.Detectada;
    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaResolucion { get; private set; }
    public string? Observacion { get; private set; }

    // Solo para EF Core al leer filas. Toda alerta nueva se crea con Detectar.
    private AlertaFraude()
    {
    }

    // Toda alerta nace en estado Detectada.
    public static AlertaFraude Detectar(string numeroTarjetaEnmascarado, decimal monto, string comercio,
        DateTime fechaTransaccionUtc, int nivelRiesgo, DateTime ahoraUtc) =>
        new()
        {
            NumeroTarjetaEnmascarado = numeroTarjetaEnmascarado,
            Monto = monto,
            Comercio = comercio,
            FechaTransaccion = fechaTransaccionUtc,
            NivelRiesgo = nivelRiesgo,
            Estado = EstadoAlerta.Detectada,
            FechaCreacion = ahoraUtc
        };

    // Solo la invoca la máquina de estados después de validar la transición (RD-04): la entidad no
    // decide qué transiciones existen ni cuáles estados son terminales.
    internal void AplicarEstado(EstadoAlerta nuevoEstado, DateTime? fechaResolucionUtc, string? observacion)
    {
        Estado = nuevoEstado;
        if (observacion is not null)
            Observacion = observacion;
        if (fechaResolucionUtc is not null)
            FechaResolucion = fechaResolucionUtc;
    }
}
