namespace Tracking_Tiger.Core.ControlAcceso;

// Cuerpo JSON de PUT /api/usuarios/{id}/rol. Admite null para dar un mensaje controlado (RD-07).
public sealed record SolicitudCambioRol(string? Rol);
