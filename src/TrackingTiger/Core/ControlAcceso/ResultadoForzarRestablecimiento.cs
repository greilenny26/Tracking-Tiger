namespace Tracking_Tiger.Core.ControlAcceso;

public enum EstadoForzarRestablecimiento
{
    Forzado,
    NoEncontrado
}

// Resultado de forzar el restablecimiento de la contraseña de un usuario (RF-CA-13).
public sealed record ResultadoForzarRestablecimiento(EstadoForzarRestablecimiento Estado, string Mensaje, UsuarioListado? Usuario)
{
    public static ResultadoForzarRestablecimiento Forzado(UsuarioListado usuario) =>
        new(EstadoForzarRestablecimiento.Forzado,
            "Se forzó el restablecimiento: la contraseña anterior ya no sirve, sus sesiones se cerraron y se le envió por correo un código para definir una nueva.",
            usuario);

    public static ResultadoForzarRestablecimiento NoEncontrado() =>
        new(EstadoForzarRestablecimiento.NoEncontrado, "No existe un usuario con ese id.", null);
}
