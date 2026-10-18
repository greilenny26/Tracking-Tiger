namespace Tracking_Tiger.Core.Permisos;

public enum EstadoCreacionSolicitud
{
    Creada,
    PermisoInvalido,
    YaPendiente
}

// Resultado de crear una solicitud de permiso (RF-GP-01).
public sealed record ResultadoCreacionSolicitud(EstadoCreacionSolicitud Estado, string Mensaje, SolicitudPermisoListado? Solicitud)
{
    public static ResultadoCreacionSolicitud Creada(SolicitudPermisoListado solicitud) =>
        new(EstadoCreacionSolicitud.Creada, "Solicitud creada en estado Pendiente.", solicitud);

    public static ResultadoCreacionSolicitud PermisoInvalido(string mensaje) =>
        new(EstadoCreacionSolicitud.PermisoInvalido, mensaje, null);

    public static ResultadoCreacionSolicitud YaPendiente() =>
        new(EstadoCreacionSolicitud.YaPendiente, "Ya tienes una solicitud pendiente para ese permiso.", null);
}
