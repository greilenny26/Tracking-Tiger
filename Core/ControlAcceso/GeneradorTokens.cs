using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Tracking_Tiger.Core.ControlAcceso;

// Genera tokens aleatorios y calcula su hash. Solo el hash se guarda en la base de datos;
// el token en claro existe únicamente en memoria hasta entregarse al usuario. Nunca se registra en logs.
public static class GeneradorTokens
{
    private const int LargoTokenBytes = 32;

    // 32 bytes aleatorios en Base64 apto para URL (sin '+', '/' ni '='), 43 caracteres.
    public static string GenerarToken() =>
        Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(LargoTokenBytes));

    // SHA-256 del token en hexadecimal en minúsculas (64 caracteres). El token tiene 256 bits
    // de entropía, así que no necesita sal y se puede buscar directamente por este valor.
    public static string CalcularHash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
