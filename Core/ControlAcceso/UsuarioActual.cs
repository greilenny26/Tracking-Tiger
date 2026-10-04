namespace Tracking_Tiger.Core.ControlAcceso;

// Datos públicos del usuario autenticado (RF-CA-07). Nunca incluye hashes ni tokens.
// El rol llega en la rama de administración de usuarios.
public sealed record UsuarioActual(int Id, string Nombre, string Correo);
