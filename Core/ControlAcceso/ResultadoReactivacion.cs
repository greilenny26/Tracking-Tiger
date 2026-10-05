namespace Tracking_Tiger.Core.ControlAcceso;

public enum EstadoReactivacion
{
    Reactivado,
    NoEncontrado,
    SinActivarPorCorreo
}

// Resultado de reactivar un usuario (RF-CA-20).
public sealed record ResultadoReactivacion(EstadoReactivacion Estado, string Mensaje, UsuarioListado? Usuario)
{
    public static ResultadoReactivacion Reactivado(UsuarioListado usuario) =>
        new(EstadoReactivacion.Reactivado, "Usuario reactivado. Debe iniciar sesión de nuevo.", usuario);

    public static ResultadoReactivacion NoEncontrado() =>
        new(EstadoReactivacion.NoEncontrado, "No existe un usuario con ese id.", null);

    // RF-CA-20: reactivar nunca se salta la activación por correo.
    public static ResultadoReactivacion SinActivarPorCorreo() =>
        new(EstadoReactivacion.SinActivarPorCorreo,
            "El usuario todavía no activó su cuenta por correo; no hay nada que reactivar.", null);
}
