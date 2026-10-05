namespace Tracking_Tiger.Core.ControlAcceso;

public enum EstadoReactivacion
{
    Reactivado,
    NoEncontrado
}

// Resultado de reactivar un usuario (RF-CA-20).
public sealed record ResultadoReactivacion(EstadoReactivacion Estado, string Mensaje, UsuarioListado? Usuario)
{
    public static ResultadoReactivacion Reactivado(UsuarioListado usuario) =>
        new(EstadoReactivacion.Reactivado, "Usuario reactivado. Debe iniciar sesión de nuevo.", usuario);

    public static ResultadoReactivacion NoEncontrado() =>
        new(EstadoReactivacion.NoEncontrado, "No existe un usuario con ese id.", null);
}
