namespace Tracking_Tiger.Core.ControlAcceso;

// Usuario del sistema (RF-CA-01). El correo es único y se guarda normalizado.
// Todas las fechas están en UTC.
public class Usuario
{
    private string _correo = string.Empty;

    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    // Se guarda siempre sin espacios a los lados y en minúsculas, así el índice único
    // trata "Ana@Example.com " y "ana@example.com" como el mismo correo.
    public string Correo
    {
        get => _correo;
        set => _correo = NormalizarCorreo(value);
    }

    public string HashContrasena { get; set; } = string.Empty;

    // El usuario nace inactivo hasta abrir el enlace de activación (RF-CA-15).
    public bool Activo { get; set; }

    public DateTime FechaCreacion { get; set; }

    // Única regla de normalización del correo; también se usa para buscar usuarios por correo.
    public static string NormalizarCorreo(string correo) =>
        (correo ?? string.Empty).Trim().ToLowerInvariant();
}
