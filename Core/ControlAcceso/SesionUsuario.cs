namespace Tracking_Tiger.Core.ControlAcceso;

// Sesión abierta de un usuario (RF-CA-03). La credencial que recibe el cliente es un token aleatorio
// (GeneradorTokens.GenerarToken); aquí solo se guarda su SHA-256 (GeneradorTokens.CalcularHash),
// nunca el valor en claro. Todas las fechas están en UTC.
public class SesionUsuario
{
    // Duración de una sesión, según el plan aprobado.
    public static readonly TimeSpan Vigencia = TimeSpan.FromHours(8);

    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime FechaEmision { get; private set; }
    public DateTime FechaVencimiento { get; private set; }
    public bool Revocada { get; private set; }

    // Solo para EF Core al leer filas. Toda sesión nueva se crea con Emitir.
    private SesionUsuario()
    {
    }

    public static SesionUsuario Emitir(Usuario usuario, string tokenHash, DateTime ahoraUtc) =>
        new()
        {
            Usuario = usuario,
            TokenHash = tokenHash,
            FechaEmision = ahoraUtc,
            FechaVencimiento = ahoraUtc + Vigencia,
            Revocada = false
        };
}
