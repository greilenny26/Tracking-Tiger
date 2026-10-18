namespace Tracking_Tiger.Core.Permisos;

// ÚNICO lugar donde se declaran los estados de una solicitud de permiso.
// Las transiciones permitidas están en MaquinaEstadosSolicitud (RD-04).
public enum EstadoSolicitud
{
    // RF-GP-01: toda solicitud nace aquí.
    Pendiente,

    // Un Administrador la aprobó (RF-GP-03); el sistema la aplica enseguida.
    Aprobada,

    // Terminal: el permiso ya fue otorgado al solicitante (RF-GP-05).
    Aplicada,

    // Terminal: un Administrador la rechazó con un motivo (RF-GP-04).
    Rechazada
}