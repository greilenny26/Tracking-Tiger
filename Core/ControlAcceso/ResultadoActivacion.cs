namespace Tracking_Tiger.Core.ControlAcceso;

public enum EstadoActivacion
{
    Activada,
    EnlaceInvalido
}

// Resultado de abrir el enlace de activación (RF-CA-16). El rechazo usa un mensaje genérico:
// no revela si el token existió ni a qué cuenta pertenece.
public sealed record ResultadoActivacion(EstadoActivacion Estado, string Mensaje)
{
    public static ResultadoActivacion Activada() =>
        new(EstadoActivacion.Activada, "Tu cuenta se activó correctamente. Ya puedes iniciar sesión.");

    public static ResultadoActivacion EnlaceInvalido() =>
        new(EstadoActivacion.EnlaceInvalido, "El enlace de activación no es válido.");
}
