namespace Tracking_Tiger.Core.ControlAcceso;

public enum EstadoRestablecimiento
{
    Restablecida,
    CodigoInvalido,
    DatosInvalidos
}

// Resultado de restablecer la contraseña con un código (RF-CA-11). Un código desconocido, usado o
// vencido recibe siempre el MISMO mensaje: no revela cuál de los tres casos fue.
public sealed record ResultadoRestablecimiento(EstadoRestablecimiento Estado, string Mensaje)
{
    public static ResultadoRestablecimiento Restablecida() =>
        new(EstadoRestablecimiento.Restablecida, "Tu contraseña se actualizó. Ya puedes iniciar sesión con la nueva.");

    public static ResultadoRestablecimiento CodigoInvalido() =>
        new(EstadoRestablecimiento.CodigoInvalido, "El código no es válido, ya fue usado o venció.");

    public static ResultadoRestablecimiento DatosInvalidos(string mensaje) =>
        new(EstadoRestablecimiento.DatosInvalidos, mensaje);
}
