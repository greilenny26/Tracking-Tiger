namespace Tracking_Tiger.Core.ControlAcceso;

public enum EstadoInicioSesion
{
    Iniciada,
    DatosInvalidos,
    CredencialesInvalidas,
    CuentaInactiva
}

// Resultado de iniciar sesión (RF-CA-03). Token es la credencial en claro: solo existe en memoria
// para devolverse en la respuesta del inicio de sesión; en la base queda únicamente su hash.
// Clase y no record a propósito: el ToString de un record imprimiría el token.
public sealed class ResultadoInicioSesion
{
    public EstadoInicioSesion Estado { get; }
    public string? Mensaje { get; }
    public string? Token { get; }
    public DateTime? VenceEn { get; }

    private ResultadoInicioSesion(EstadoInicioSesion estado, string? mensaje, string? token, DateTime? venceEn)
    {
        Estado = estado;
        Mensaje = mensaje;
        Token = token;
        VenceEn = venceEn;
    }

    public static ResultadoInicioSesion Iniciada(string token, DateTime venceEnUtc) =>
        new(EstadoInicioSesion.Iniciada, null, token, venceEnUtc);

    public static ResultadoInicioSesion DatosInvalidos(string mensaje) =>
        new(EstadoInicioSesion.DatosInvalidos, mensaje, null, null);

    // Mismo mensaje para correo inexistente y contraseña incorrecta: no revela cuál falló.
    public static ResultadoInicioSesion CredencialesInvalidas() =>
        new(EstadoInicioSesion.CredencialesInvalidas, "Credenciales inválidas.", null, null);

    // Solo se usa con la contraseña ya verificada (RF-CA-15).
    public static ResultadoInicioSesion CuentaInactiva() =>
        new(EstadoInicioSesion.CuentaInactiva, "La cuenta no está activa. Revisa tu correo para activarla.", null, null);
}
