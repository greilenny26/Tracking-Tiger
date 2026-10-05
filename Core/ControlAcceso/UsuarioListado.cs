namespace Tracking_Tiger.Core.ControlAcceso;

// Fila del listado de usuarios para el Administrador (RF-CA-21). Solo datos públicos:
// NUNCA hash de contraseña, tokens, intentos fallidos, bloqueo ni ningún otro campo interno.
// Agregar un campo aquí es la única forma de que aparezca en la respuesta.
public sealed record UsuarioListado(int Id, string Nombre, string Correo, string Rol, bool Activo);
