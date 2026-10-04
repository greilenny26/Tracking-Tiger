namespace Tracking_Tiger.Core.Correo;

// Error al conectar o enviar, con un mensaje en español apto para mostrar.
// El mensaje nunca incluye credenciales, respuestas del servidor ni detalles internos (RD-08).
// DetieneElLote indica que los demás correos también fallarían (servidor inalcanzable, credenciales
// rechazadas), así que no tiene sentido seguir intentando en esta ejecución.
public sealed class ErrorEnvioCorreo : Exception
{
    public bool DetieneElLote { get; }

    public ErrorEnvioCorreo(string mensaje, bool detieneElLote)
        : base(mensaje)
    {
        DetieneElLote = detieneElLote;
    }
}
