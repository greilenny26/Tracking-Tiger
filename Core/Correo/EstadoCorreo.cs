namespace Tracking_Tiger.Core.Correo;

// Estados posibles de un correo en la cola. Se guardan como texto en la base de datos.
public enum EstadoCorreo
{
    Pendiente,
    Enviado,
    Fallido
}
