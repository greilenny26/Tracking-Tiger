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
    // Solo la propia entidad puede cambiarlo; desde fuera es de solo lectura.
    public bool Activo { get; private set; }

    // Intentos fallidos de inicio de sesión (RF-CA-19). Solo lo incrementa el servicio de sesión
    // con una actualización atómica en la base; el bloqueo y el reinicio llegan en pasos aparte.
    public int IntentosFallidos { get; private set; }

    // Fin del bloqueo temporal por intentos fallidos (RF-CA-19), en UTC. Null = sin bloqueo.
    public DateTime? BloqueadoHasta { get; private set; }

    public DateTime FechaCreacion { get; set; }

    // Solo para EF Core al leer filas. Todo usuario nuevo se crea con CrearNuevo.
    private Usuario()
    {
    }

    // Activa la cuenta al abrir el enlace de activación (RF-CA-16).
    public void Activar() => Activo = true;

    // Regla de dominio (RF-CA-15): un usuario nuevo SIEMPRE nace inactivo.
    // El correo se normaliza al asignarlo; el hash ya debe venir calculado.
    public static Usuario CrearNuevo(string nombre, string correo, string hashContrasena, DateTime fechaCreacionUtc) =>
        new()
        {
            Nombre = nombre,
            Correo = correo,
            HashContrasena = hashContrasena,
            Activo = false,
            FechaCreacion = fechaCreacionUtc
        };

    // Única regla de normalización del correo; también se usa para buscar usuarios por correo.
    public static string NormalizarCorreo(string correo) =>
        (correo ?? string.Empty).Trim().ToLowerInvariant();
}
