using System.Net.Mail;
using Tracking_Tiger.Core.Comun;

namespace Tracking_Tiger.Core.ControlAcceso;

// Valida el formato del correo antes de usarlo en el registro (RD-07).
// Nunca lanza: todo rechazo vuelve como ResultadoValidacion con un mensaje en español.
public static class ValidadorCorreo
{
    // Largo máximo práctico de una dirección de correo (RFC 5321).
    public const int LargoMaximo = 254;

    public static ResultadoValidacion Validar(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
            return ResultadoValidacion.Rechazado("El correo es obligatorio.");

        // Se valida el mismo valor que se guardará: sin espacios a los lados.
        correo = correo.Trim();

        if (correo.Length > LargoMaximo)
            return ResultadoValidacion.Rechazado($"El correo no puede tener más de {LargoMaximo} caracteres.");

        // MailAddress también acepta formas como "Ana <ana@example.com>"; exigir que la
        // dirección leída sea igual a la entrada deja pasar solo la dirección sola.
        if (!MailAddress.TryCreate(correo, out var direccion) || direccion.Address != correo)
            return ResultadoValidacion.Rechazado("El correo no tiene un formato válido.");

        var dominio = direccion.Host;
        if (!dominio.Contains('.') || dominio.StartsWith('.') || dominio.EndsWith('.'))
            return ResultadoValidacion.Rechazado("El correo no tiene un formato válido.");

        return ResultadoValidacion.Correcto();
    }
}
