using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.Correo;

// Registra correos en CorreoEnCola con estado Pendiente (RF-NOT-08).
// No envía nada: la operación que origina el correo termina bien aunque el servidor SMTP no responda.
public sealed class ColaCorreo : IColaCorreo
{
    // Deben coincidir con los límites declarados en ContextoDatos.
    private const int LargoMaximoDestinatario = 320;
    private const int LargoMaximoAsunto = 200;

    private readonly ContextoDatos _contexto;
    private readonly IReloj _reloj;

    public ColaCorreo(ContextoDatos contexto, IReloj reloj)
    {
        _contexto = contexto;
        _reloj = reloj;
    }

    public async Task<ResultadoEncolar> EncolarAsync(string destinatario, string asunto, string cuerpo)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
            return ResultadoEncolar.Rechazado("El destinatario es obligatorio.");
        if (string.IsNullOrWhiteSpace(asunto))
            return ResultadoEncolar.Rechazado("El asunto es obligatorio.");
        if (string.IsNullOrWhiteSpace(cuerpo))
            return ResultadoEncolar.Rechazado("El cuerpo es obligatorio.");

        destinatario = destinatario.Trim();
        asunto = asunto.Trim();

        if (destinatario.Length > LargoMaximoDestinatario)
            return ResultadoEncolar.Rechazado("El destinatario es demasiado largo.");
        if (asunto.Length > LargoMaximoAsunto)
            return ResultadoEncolar.Rechazado("El asunto es demasiado largo.");

        var correo = new CorreoEnCola
        {
            Destinatario = destinatario,
            Asunto = asunto,
            Cuerpo = cuerpo,
            Estado = EstadoCorreo.Pendiente,
            Intentos = 0,
            FechaCreacion = _reloj.AhoraUtc
        };

        _contexto.CorreosEnCola.Add(correo);
        await _contexto.SaveChangesAsync();

        return ResultadoEncolar.Aceptado(correo.Id);
    }
}
