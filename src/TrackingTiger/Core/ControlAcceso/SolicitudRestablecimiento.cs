namespace Tracking_Tiger.Core.ControlAcceso;

// Cuerpo JSON de POST /api/auth/restablecer. Admite null para dar mensajes controlados (RD-07).
public sealed record SolicitudRestablecimiento(string? Codigo, string? NuevaContrasena);
