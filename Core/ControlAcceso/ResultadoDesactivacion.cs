namespace Tracking_Tiger.Core.ControlAcceso;

public enum EstadoDesactivacion
{
    Desactivado,
    NoEncontrado,
    PropioUsuario
}

// Resultado de desactivar un usuario (RF-CA-20).
public sealed record ResultadoDesactivacion(EstadoDesactivacion Estado, string Mensaje, UsuarioListado? Usuario)
{
    public static ResultadoDesactivacion Desactivado(UsuarioListado usuario) =>
        new(EstadoDesactivacion.Desactivado, "Usuario desactivado. Sus sesiones abiertas se cerraron.", usuario);

    public static ResultadoDesactivacion NoEncontrado() =>
        new(EstadoDesactivacion.NoEncontrado, "No existe un usuario con ese id.", null);

    // RF-CA-20: un Administrador no puede desactivarse a sí mismo.
    public static ResultadoDesactivacion PropioUsuario() =>
        new(EstadoDesactivacion.PropioUsuario, "No puedes desactivar tu propia cuenta.", null);
}
