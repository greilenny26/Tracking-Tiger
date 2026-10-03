namespace Tracking_Tiger.Core.Correo;

// Envía correos a un servidor real. Abre una sesión por lote para no conectarse una vez por correo.
public interface IEnviadorCorreo
{
    Task<ISesionCorreo> AbrirSesionAsync(ConfiguracionSmtp configuracion);
}

// Conexión abierta y autenticada con el servidor. Al liberarla se desconecta.
public interface ISesionCorreo : IAsyncDisposable
{
    Task EnviarAsync(CorreoEnCola correo);
}
