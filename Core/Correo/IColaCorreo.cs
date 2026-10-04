namespace Tracking_Tiger.Core.Correo;

// Cola de correos salientes (RF-NOT-08). Encolar solo registra el correo;
// nunca contacta al servidor SMTP. El envío lo hace un proceso aparte.
public interface IColaCorreo
{
    Task<ResultadoEncolar> EncolarAsync(string destinatario, string asunto, string cuerpo);
}
