namespace Tracking_Tiger.Core.ControlAcceso;

// Token del enlace de activación (RF-CA-15): de un solo uso y con vencimiento.
// Solo se guarda el SHA-256 del token, nunca el valor en claro. Todas las fechas están en UTC.
public class TokenActivacion
{
    public static readonly TimeSpan Vigencia = TimeSpan.FromHours(24);

    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime FechaEmision { get; private set; }
    public DateTime FechaVencimiento { get; private set; }
    public bool Usado { get; private set; }

    // Solo para EF Core al leer filas. Todo token nuevo se crea con Emitir.
    private TokenActivacion()
    {
    }

    // Se asocia por la navegación para guardarse en el mismo SaveChanges que un usuario nuevo,
    // cuando su Id todavía no existe.
    public static TokenActivacion Emitir(Usuario usuario, string tokenHash, DateTime ahoraUtc) =>
        new()
        {
            Usuario = usuario,
            TokenHash = tokenHash,
            FechaEmision = ahoraUtc,
            FechaVencimiento = ahoraUtc + Vigencia,
            Usado = false
        };
}
