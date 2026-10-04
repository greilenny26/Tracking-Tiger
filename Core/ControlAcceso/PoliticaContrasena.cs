using Tracking_Tiger.Core.Comun;

namespace Tracking_Tiger.Core.ControlAcceso;

// Política mínima de contraseña (RF-CA-14): al menos 8 caracteres, con letras y números.
// Único punto donde vive la regla; la usan el registro, la recuperación y todo cambio de contraseña.
// Nunca lanza: todo rechazo vuelve como ResultadoValidacion con un mensaje en español (RD-07).
public static class PoliticaContrasena
{
    public const int LargoMinimo = 8;

    public static ResultadoValidacion Validar(string? contrasena)
    {
        // No se recortan espacios: forman parte de la contraseña que escribió el usuario.
        if (string.IsNullOrEmpty(contrasena))
            return ResultadoValidacion.Rechazado("La contraseña es obligatoria.");

        if (contrasena.Length < LargoMinimo)
            return ResultadoValidacion.Rechazado($"La contraseña debe tener al menos {LargoMinimo} caracteres.");

        // Cuenta como letra cualquier letra, incluidas las acentuadas y la ñ.
        if (!contrasena.Any(char.IsLetter))
            return ResultadoValidacion.Rechazado("La contraseña debe incluir al menos una letra.");

        if (!contrasena.Any(char.IsAsciiDigit))
            return ResultadoValidacion.Rechazado("La contraseña debe incluir al menos un número.");

        return ResultadoValidacion.Correcto();
    }
}
