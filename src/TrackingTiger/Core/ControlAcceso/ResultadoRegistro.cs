namespace Tracking_Tiger.Core.ControlAcceso;

public enum EstadoRegistro
{
    Registrado,
    DatosInvalidos,
    CorreoDuplicado,
    NoDisponible
}

// Datos públicos del usuario recién registrado. Nunca incluye el hash de la contraseña.
public sealed record UsuarioRegistrado(int Id, string Nombre, string Correo, bool Activo);

// Resultado de intentar registrar un usuario (RF-CA-01). Los rechazos son controlados (RD-07).
// NoDisponible: falta configuración del servidor (por ejemplo, APP_URL_BASE); el detalle
// queda en el log del servidor y al usuario solo le llega un mensaje genérico (RD-08).
public sealed record ResultadoRegistro(EstadoRegistro Estado, string Mensaje, UsuarioRegistrado? Usuario)
{
    public static ResultadoRegistro Registrado(UsuarioRegistrado usuario) =>
        new(EstadoRegistro.Registrado,
            "La cuenta se creó, pero todavía no está activa. Te enviaremos un correo con el enlace para activarla.",
            usuario);

    public static ResultadoRegistro DatosInvalidos(string mensaje) =>
        new(EstadoRegistro.DatosInvalidos, mensaje, null);

    public static ResultadoRegistro CorreoDuplicado() =>
        new(EstadoRegistro.CorreoDuplicado, "Ya existe una cuenta registrada con ese correo.", null);

    public static ResultadoRegistro NoDisponible() =>
        new(EstadoRegistro.NoDisponible,
            "El registro no está disponible en este momento. Intenta de nuevo más tarde.", null);
}
