namespace Tracking_Tiger.Core.ControlAcceso;

public enum EstadoSolicitudRecuperacion
{
    Aceptada,
    DatosInvalidos
}

// Resultado de pedir la recuperación de contraseña (RF-CA-09). Aceptada es la MISMA respuesta
// exista o no el correo: el flujo no revela qué correos están registrados.
public sealed record ResultadoSolicitudRecuperacion(EstadoSolicitudRecuperacion Estado, string Mensaje)
{
    public static ResultadoSolicitudRecuperacion Aceptada() =>
        new(EstadoSolicitudRecuperacion.Aceptada,
            "Si el correo está registrado, recibirás instrucciones para recuperar tu contraseña.");

    public static ResultadoSolicitudRecuperacion DatosInvalidos(string mensaje) =>
        new(EstadoSolicitudRecuperacion.DatosInvalidos, mensaje);
}
