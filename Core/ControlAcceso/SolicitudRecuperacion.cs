namespace Tracking_Tiger.Core.ControlAcceso;

// Cuerpo JSON de POST /api/auth/recuperar. Admite null para que el validador dé el mensaje (RD-07).
public sealed record SolicitudRecuperacion(string? Correo);
