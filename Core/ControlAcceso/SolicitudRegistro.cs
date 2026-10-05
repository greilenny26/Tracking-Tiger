namespace Tracking_Tiger.Core.ControlAcceso;

// Cuerpo JSON de POST /api/auth/registro.
// Los campos admiten null para que ServicioRegistro dé el mensaje de cada caso (RD-07)
// en lugar del rechazo genérico del enlace de modelos.
// A propósito NO tiene campo de rol (RF-CA-04): un "rol" enviado en el JSON se ignora;
// el rol lo asigna siempre Usuario.CrearNuevo (Estándar).
public sealed record SolicitudRegistro(string? Nombre, string? Correo, string? Contrasena);
