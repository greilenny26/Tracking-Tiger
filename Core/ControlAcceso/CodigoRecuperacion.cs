namespace Tracking_Tiger.Core.ControlAcceso;

// Código de recuperación de contraseña (RF-CA-10): de un solo uso y con vencimiento.
// Solo se guarda el SHA-256 del código (GeneradorTokens.CalcularHash), nunca el valor en claro.
// Todas las fechas están en UTC.
public class CodigoRecuperacion
{
    // Vigencia del código, según el plan aprobado.
    public static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(15);

    public int Id { get; private set; }
    public int UsuarioId { get; private set; }
    public Usuario Usuario { get; private set; } = null!;
    public string CodigoHash { get; private set; } = string.Empty;
    public DateTime FechaEmision { get; private set; }
    public DateTime FechaVencimiento { get; private set; }
    public bool Usado { get; private set; }

    // Solo para EF Core al leer filas. Todo código nuevo se crea con Emitir.
    private CodigoRecuperacion()
    {
    }

    public static CodigoRecuperacion Emitir(Usuario usuario, string codigoHash, DateTime ahoraUtc) =>
        new()
        {
            Usuario = usuario,
            CodigoHash = codigoHash,
            FechaEmision = ahoraUtc,
            FechaVencimiento = ahoraUtc + Vigencia,
            Usado = false
        };
}
