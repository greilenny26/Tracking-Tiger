namespace Tracking_Tiger.Core.ControlAcceso;

// Datos públicos del usuario autenticado (RF-CA-07). Nunca incluye hashes ni tokens.
public sealed record UsuarioActual(int Id, string Nombre, string Correo, Rol Rol);
