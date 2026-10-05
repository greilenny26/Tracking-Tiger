namespace Tracking_Tiger.Core.ControlAcceso;

// Cuerpo JSON de POST /api/auth/cambiar-contrasena. Admite null para dar mensajes controlados (RD-07).
public sealed record SolicitudCambioContrasena(string? ContrasenaActual, string? NuevaContrasena);
