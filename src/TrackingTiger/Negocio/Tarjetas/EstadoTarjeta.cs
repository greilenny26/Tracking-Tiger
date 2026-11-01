namespace Tracking_Tiger.Negocio.Tarjetas;

// RF-NEG-03: ÚNICO lugar donde se declaran los estados de una tarjeta (4 estados).
// Las transiciones permitidas están en MaquinaEstadosTarjeta (RD-04).
public enum EstadoTarjeta
{
    // Opera con normalidad. Toda tarjeta nace aquí.
    Activa,

    // Bloqueo temporal por actividad sospechosa; se levanta si el cliente reconoce la actividad.
    BloqueoSuave,

    // El cliente no reconoció la actividad; solo un agente con permiso aprobado lo levanta.
    BloqueoTotal,

    // Terminal: pérdida o robo. Nunca vuelve a Activa (RF-NEG-05).
    Cancelada
}
