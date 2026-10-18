using System.Net.Sockets;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Tracking_Tiger.Core.Correo;

// Implementación de IEnviadorCorreo con MailKit. Se conecta con STARTTLS.
// Es el único lugar del proyecto que habla con el servidor SMTP.
// Toda excepción de MailKit se traduce a ErrorEnvioCorreo con un mensaje fijo:
// nunca se muestra el texto original, que puede incluir respuestas del servidor.
public sealed class EnviadorSmtp : IEnviadorCorreo
{
    // Límite para conectar y autenticarse, y para cada operación de red, así el proceso nunca se cuelga.
    private static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(15);

    public async Task<ISesionCorreo> AbrirSesionAsync(ConfiguracionSmtp configuracion)
    {
        if (!MailboxAddress.TryParse(configuracion.Remitente, out var remitente))
            throw new ErrorEnvioCorreo(
                $"La variable de entorno {ConfiguracionSmtp.VariableRemitente} (o {ConfiguracionSmtp.VariableUsuario}) no es una dirección de correo válida.",
                detieneElLote: true);

        var sesion = new SesionSmtp(configuracion, remitente);
        try
        {
            await sesion.ConectarAsync();
            return sesion;
        }
        catch
        {
            await sesion.DisposeAsync();
            throw;
        }
    }

    private sealed class SesionSmtp : ISesionCorreo
    {
        private readonly ConfiguracionSmtp _configuracion;
        private readonly MailboxAddress _remitente;
        private readonly SmtpClient _cliente = new() { Timeout = (int)TiempoMaximo.TotalMilliseconds };

        public SesionSmtp(ConfiguracionSmtp configuracion, MailboxAddress remitente)
        {
            _configuracion = configuracion;
            _remitente = remitente;
        }

        public async Task ConectarAsync()
        {
            using var limite = new CancellationTokenSource(TiempoMaximo);
            try
            {
                await _cliente.ConnectAsync(_configuracion.Host, _configuracion.Puerto, SecureSocketOptions.StartTls, limite.Token);
                await _cliente.AuthenticateAsync(_configuracion.Usuario, _configuracion.Clave, limite.Token);
            }
            catch (Exception error) when (error is not ErrorEnvioCorreo)
            {
                throw TraducirErrorDeConexion(error);
            }
        }

        public async Task EnviarAsync(CorreoEnCola correo)
        {
            // Si un envío anterior cortó la conexión, se reconecta antes de seguir.
            if (!_cliente.IsConnected)
                await ConectarAsync();

            if (!MailboxAddress.TryParse(correo.Destinatario, out var destinatario))
                throw new ErrorEnvioCorreo("La dirección del destinatario no es válida.", detieneElLote: false);

            var mensaje = new MimeMessage();
            mensaje.From.Add(_remitente);
            mensaje.To.Add(destinatario);
            mensaje.Subject = correo.Asunto;
            mensaje.Body = new TextPart("plain") { Text = correo.Cuerpo };

            try
            {
                await _cliente.SendAsync(mensaje);
            }
            catch (Exception error)
            {
                throw TraducirErrorDeEnvio(error);
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_cliente.IsConnected)
                    await _cliente.DisconnectAsync(true);
            }
            catch
            {
                // La conexión ya estaba rota; no hay nada más que cerrar.
            }
            _cliente.Dispose();
        }

        private static ErrorEnvioCorreo TraducirErrorDeConexion(Exception error) => error switch
        {
            OperationCanceledException or TimeoutException =>
                new("El servidor SMTP no respondió en 15 segundos. Revisa SMTP_HOST, SMTP_PORT y la conexión a internet.", true),
            SocketException =>
                new("No se pudo conectar con el servidor SMTP. Revisa SMTP_HOST, SMTP_PORT y la conexión a internet.", true),
            SslHandshakeException or NotSupportedException =>
                new("No se pudo establecer la conexión segura (STARTTLS) con el servidor SMTP. Revisa SMTP_PORT (normalmente 587).", true),
            AuthenticationException =>
                new("El servidor SMTP rechazó el usuario o la clave. Revisa SMTP_USUARIO y SMTP_CLAVE.", true),
            _ =>
                new("El servidor SMTP cerró o rechazó la conexión.", true)
        };

        private static ErrorEnvioCorreo TraducirErrorDeEnvio(Exception error) => error switch
        {
            SmtpCommandException { ErrorCode: SmtpErrorCode.RecipientNotAccepted } =>
                new("El servidor SMTP rechazó al destinatario.", false),
            SmtpCommandException { ErrorCode: SmtpErrorCode.SenderNotAccepted } =>
                new("El servidor SMTP rechazó al remitente. Revisa SMTP_REMITENTE.", false),
            SmtpCommandException =>
                new("El servidor SMTP rechazó el mensaje.", false),
            OperationCanceledException or TimeoutException =>
                new("El servidor SMTP no respondió a tiempo durante el envío.", false),
            _ =>
                new("Se perdió la conexión con el servidor SMTP durante el envío.", false)
        };
    }
}
