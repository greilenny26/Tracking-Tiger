namespace Tracking_Tiger.Core.ControlAcceso;

public enum EstadoRegistro
{
    Registrado,
    DatosInvalidos,
    CorreoDuplicado
}

// Datos públicos del usuario recién registrado. Nunca incluye el hash de la contraseña.
public sealed record UsuarioRegistrado(int Id, string Nombre, string Correo, bool Activo);

// Resultado de intentar registrar un usuario (RF-CA-01). Los rechazos son controlados (RD-07).
// TokenActivacionPlano es el token en claro para armar el enlace de activación (RF-CA-15):
// solo existe en memoria, nunca va en la respuesta HTTP ni en logs.
// Es una clase y no un record a propósito: el ToString de un record imprimiría el token.
public sealed class ResultadoRegistro
{
    public EstadoRegistro Estado { get; }
    public string Mensaje { get; }
    public UsuarioRegistrado? Usuario { get; }
    public string? TokenActivacionPlano { get; }

    private ResultadoRegistro(EstadoRegistro estado, string mensaje, UsuarioRegistrado? usuario, string? tokenActivacionPlano)
    {
        Estado = estado;
        Mensaje = mensaje;
        Usuario = usuario;
        TokenActivacionPlano = tokenActivacionPlano;
    }

    public static ResultadoRegistro Registrado(UsuarioRegistrado usuario, string tokenActivacionPlano) =>
        new(EstadoRegistro.Registrado, "La cuenta se creó, pero todavía no está activa.", usuario, tokenActivacionPlano);

    public static ResultadoRegistro DatosInvalidos(string mensaje) =>
        new(EstadoRegistro.DatosInvalidos, mensaje, null, null);

    public static ResultadoRegistro CorreoDuplicado() =>
        new(EstadoRegistro.CorreoDuplicado, "Ya existe una cuenta registrada con ese correo.", null, null);
}
