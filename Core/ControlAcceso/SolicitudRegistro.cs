namespace Tracking_Tiger.Core.ControlAcceso;

// Cuerpo JSON de POST /api/auth/registro.
// Los campos admiten null para que ServicioRegistro dé el mensaje de cada caso (RD-07)
// en lugar del rechazo genérico del enlace de modelos.
public sealed record SolicitudRegistro(string? Nombre, string? Correo, string? Contrasena);
