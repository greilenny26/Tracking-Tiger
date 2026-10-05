namespace Tracking_Tiger.Negocio.Alertas;

// RF-NEG-03: ÚNICO lugar donde se declaran los estados de una alerta de fraude (4 estados).
// Las transiciones permitidas están en MaquinaEstadosAlerta (RD-04).
public enum EstadoAlerta
{
    // La alerta nace aquí cuando se detecta una transacción sospechosa.
    Detectada,

    // Un analista la está revisando.
    EnRevision,

    // Terminal: se confirmó que la transacción es fraude.
    Confirmada,

    // Terminal: se descartó (no era fraude).
    Descartada
}
