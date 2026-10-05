namespace Tracking_Tiger.Core.ControlAcceso;

public enum EstadoCambioContrasena
{
    Cambiada,
    DatosInvalidos,
    ContrasenaActualIncorrecta
}

// Resultado de cambiar la contraseña propia con sesión (RF-CA-22).
public sealed record ResultadoCambioContrasena(EstadoCambioContrasena Estado, string Mensaje)
{
    public static ResultadoCambioContrasena Cambiada() =>
        new(EstadoCambioContrasena.Cambiada,
            "Tu contraseña se actualizó. Por seguridad se cerraron todas tus sesiones: inicia sesión de nuevo.");

    public static ResultadoCambioContrasena DatosInvalidos(string mensaje) =>
        new(EstadoCambioContrasena.DatosInvalidos, mensaje);

    public static ResultadoCambioContrasena ContrasenaActualIncorrecta() =>
        new(EstadoCambioContrasena.ContrasenaActualIncorrecta, "La contraseña actual no es correcta.");
}
