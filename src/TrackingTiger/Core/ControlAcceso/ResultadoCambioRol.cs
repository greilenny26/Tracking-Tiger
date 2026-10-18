namespace Tracking_Tiger.Core.ControlAcceso;

public enum EstadoCambioRol
{
    Cambiado,
    RolInvalido,
    NoEncontrado,
    PropioUsuario
}

// Resultado de cambiar el rol de un usuario (RF-CA-08).
public sealed record ResultadoCambioRol(EstadoCambioRol Estado, string Mensaje, UsuarioListado? Usuario)
{
    public static ResultadoCambioRol Cambiado(UsuarioListado usuario) =>
        new(EstadoCambioRol.Cambiado, "Rol actualizado.", usuario);

    public static ResultadoCambioRol RolInvalido(string mensaje) =>
        new(EstadoCambioRol.RolInvalido, mensaje, null);

    public static ResultadoCambioRol NoEncontrado() =>
        new(EstadoCambioRol.NoEncontrado, "No existe un usuario con ese id.", null);

    // Plan aprobado: un Administrador no puede cambiar su propio rol (evita quedarse sin Administradores).
    public static ResultadoCambioRol PropioUsuario() =>
        new(EstadoCambioRol.PropioUsuario, "No puedes cambiar tu propio rol.", null);
}
