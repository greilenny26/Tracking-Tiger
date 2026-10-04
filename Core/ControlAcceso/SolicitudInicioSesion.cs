namespace Tracking_Tiger.Core.ControlAcceso;

// Cuerpo JSON de POST /api/auth/login.
// Los campos admiten null para que ServicioSesion dé el mensaje de cada caso (RD-07)
// en lugar del rechazo genérico del enlace de modelos.
public sealed record SolicitudInicioSesion(string? Correo, string? Contrasena);
