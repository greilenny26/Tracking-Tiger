namespace Tracking_Tiger.Core.ControlAcceso;

public enum EstadoReenvio
{
    Aceptado,
    DatosInvalidos,
    NoDisponible
}

// Resultado de pedir el reenvío del enlace de activación (RF-CA-17).
// Aceptado es la misma respuesta exista o no el correo, o esté ya activa la cuenta.
public sealed record ResultadoReenvio(EstadoReenvio Estado, string Mensaje)
{
    public static ResultadoReenvio Aceptado() =>
        new(EstadoReenvio.Aceptado,
            "Si el correo está registrado y la cuenta no está activa, recibirás un nuevo enlace.");

    public static ResultadoReenvio DatosInvalidos(string mensaje) =>
        new(EstadoReenvio.DatosInvalidos, mensaje);

    public static ResultadoReenvio NoDisponible() =>
        new(EstadoReenvio.NoDisponible,
            "El servicio no está disponible en este momento. Intenta de nuevo más tarde.");
}
