namespace Tracking_Tiger.Core.ControlAcceso;

public enum EstadoRegistro
{
    Registrado,
    DatosInvalidos,
    CorreoDuplicado
}

// Datos públicos del usuario recién registrado. Nunca incluye el hash de la contraseña.
public sealed record UsuarioRegistrado(int Id, string Nombre, string Correo);

// Resultado de intentar registrar un usuario (RF-CA-01). Los rechazos son controlados (RD-07).
public sealed record ResultadoRegistro(EstadoRegistro Estado, string Mensaje, UsuarioRegistrado? Usuario)
{
    public static ResultadoRegistro Registrado(UsuarioRegistrado usuario) =>
        new(EstadoRegistro.Registrado, "La cuenta se registró correctamente.", usuario);

    public static ResultadoRegistro DatosInvalidos(string mensaje) =>
        new(EstadoRegistro.DatosInvalidos, mensaje, null);

    public static ResultadoRegistro CorreoDuplicado() =>
        new(EstadoRegistro.CorreoDuplicado, "Ya existe una cuenta registrada con ese correo.", null);
}
