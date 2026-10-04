namespace Tracking_Tiger.Core.Correo;

// Cola de correos salientes (RF-NOT-08). Encolar solo registra el correo;
// nunca contacta al servidor SMTP. El envío lo hace un proceso aparte.
public interface IColaCorreo
{
    // Registra el correo y lo guarda de inmediato.
    Task<ResultadoEncolar> EncolarAsync(string destinatario, string asunto, string cuerpo);

    // Registra el correo en el contexto SIN guardarlo: queda en la misma transacción
    // que el SaveChanges de quien lo llama (o se guardan ambos o ninguno).
    ResultadoEncolar Agregar(string destinatario, string asunto, string cuerpo);
}
