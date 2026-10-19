namespace Tracking_Tiger.Core.Permisos;

public enum EstadoResolucion
{
    Resuelta,
    NoEncontrada,
    // La máquina de estados no permite el cambio (RF-GP-04, RF-GP-06, RF-GP-07): la solicitud no cambió.
    NoPermitida,
    DatoInvalido
}

// Resultado de aprobar o rechazar una solicitud de permiso.
public sealed record ResultadoResolucionSolicitud(EstadoResolucion Estado, string Mensaje, SolicitudPermisoListado? Solicitud)
{
    public static ResultadoResolucionSolicitud Resuelta(SolicitudPermisoListado solicitud) =>
        new(EstadoResolucion.Resuelta, $"La solicitud quedó en {solicitud.Estado}.", solicitud);

    public static ResultadoResolucionSolicitud NoEncontrada() =>
        new(EstadoResolucion.NoEncontrada, "No existe una solicitud con ese id.", null);

    public static ResultadoResolucionSolicitud NoPermitida(string motivo) =>
        new(EstadoResolucion.NoPermitida, motivo, null);

    public static ResultadoResolucionSolicitud DatoInvalido(string mensaje) =>
        new(EstadoResolucion.DatoInvalido, mensaje, null);
}
