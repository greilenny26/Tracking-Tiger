using System.Globalization;
using System.Security.Cryptography;

namespace Tracking_Tiger.Core.ControlAcceso;

// Hash de contraseñas con PBKDF2-HMAC-SHA256 y una sal aleatoria por contraseña (RF-CA-02, RD-05).
// Formato guardado, en una sola cadena: PBKDF2-SHA256$<iteraciones>$<sal base64>$<hash base64>.
// Las iteraciones viajan dentro del valor, así se pueden subir más adelante sin romper
// los hashes ya guardados. La misma contraseña produce un valor distinto cada vez.
public sealed class HasherContrasenasPbkdf2 : IHasherContrasenas
{
    private const string Algoritmo = "PBKDF2-SHA256";
    private const char Separador = '$';
    private const int Iteraciones = 600_000;
    private const int LargoSal = 16;
    private const int LargoHash = 32;

    public string Hashear(string contrasena)
    {
        ArgumentNullException.ThrowIfNull(contrasena);

        var sal = RandomNumberGenerator.GetBytes(LargoSal);
        var hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256, LargoHash);

        return string.Join(Separador,
            Algoritmo,
            Iteraciones.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(sal),
            Convert.ToBase64String(hash));
    }

    // Devuelve false (nunca lanza) si el valor guardado está vacío o mal formado,
    // para que un dato dañado no termine en un error 500 durante el inicio de sesión.
    public bool Verificar(string contrasena, string hashGuardado)
    {
        if (contrasena is null || string.IsNullOrEmpty(hashGuardado))
            return false;

        var partes = hashGuardado.Split(Separador);
        if (partes.Length != 4 || partes[0] != Algoritmo)
            return false;

        if (!int.TryParse(partes[1], NumberStyles.None,
                CultureInfo.InvariantCulture, out var iteraciones) || iteraciones <= 0)
            return false;

        byte[] sal;
        byte[] esperado;
        try
        {
            sal = Convert.FromBase64String(partes[2]);
            esperado = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (sal.Length < LargoSal || esperado.Length == 0)
            return false;

        var calculado = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);

        // Comparación en tiempo constante: no revela cuántos bytes coinciden.
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}
