using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Tracking_Tiger.Core.Correo;

// Implementación de IEnviadorCorreo con MailKit. Se conecta con STARTTLS.
// Es el único lugar del proyecto que habla con el servidor SMTP.
public sealed class EnviadorSmtp : IEnviadorCorreo
{
    public async Task<ISesionCorreo> AbrirSesionAsync(ConfiguracionSmtp configuracion)
    {
        var cliente = new SmtpClient();
        try
        {
            await cliente.ConnectAsync(configuracion.Host, configuracion.Puerto, SecureSocketOptions.StartTls);
            await cliente.AuthenticateAsync(configuracion.Usuario, configuracion.Clave);
            return new SesionSmtp(cliente, MailboxAddress.Parse(configuracion.Remitente));
        }
        catch
        {
            cliente.Dispose();
            throw;
        }
    }

    private sealed class SesionSmtp : ISesionCorreo
    {
        private readonly SmtpClient _cliente;
        private readonly MailboxAddress _remitente;

        public SesionSmtp(SmtpClient cliente, MailboxAddress remitente)
        {
            _cliente = cliente;
            _remitente = remitente;
        }

        public async Task EnviarAsync(CorreoEnCola correo)
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(_remitente);
            mensaje.To.Add(MailboxAddress.Parse(correo.Destinatario));
            mensaje.Subject = correo.Asunto;
            mensaje.Body = new TextPart("plain") { Text = correo.Cuerpo };

            await _cliente.SendAsync(mensaje);
        }

        public async ValueTask DisposeAsync()
        {
            if (_cliente.IsConnected)
                await _cliente.DisconnectAsync(true);
            _cliente.Dispose();
        }
    }
}
