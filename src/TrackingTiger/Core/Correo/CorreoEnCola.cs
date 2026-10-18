namespace Tracking_Tiger.Core.Correo;

// Correo registrado para que un proceso aparte lo envíe (RF-NOT-08).
// Todas las fechas están en UTC.
public class CorreoEnCola
{
    public int Id { get; set; }
    public string Destinatario { get; set; } = string.Empty;
    public string Asunto { get; set; } = string.Empty;
    public string Cuerpo { get; set; } = string.Empty;
    public EstadoCorreo Estado { get; set; } = EstadoCorreo.Pendiente;
    public int Intentos { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public string? UltimoError { get; set; }
}
