namespace Tracking_Tiger.Core.ControlAcceso;

// Cuerpo JSON de POST /api/auth/reenviar-activacion.
// El campo admite null para que el validador dé el mensaje del caso (RD-07).
public sealed record SolicitudReenvioActivacion(string? Correo);
