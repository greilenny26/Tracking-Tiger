using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.ControlAcceso;

// Activación de cuentas por enlace (RF-CA-16). Toda la regla vive aquí, no en el controlador (RD-02).
// Un token inexistente o ya usado se rechaza sin cambiar nada.
public sealed class ServicioActivacion
{
    private readonly ContextoDatos _contexto;

    public ServicioActivacion(ContextoDatos contexto)
    {
        _contexto = contexto;
    }

    public async Task<ResultadoActivacion> ActivarAsync(string? tokenPlano)
    {
        if (string.IsNullOrWhiteSpace(tokenPlano))
            return ResultadoActivacion.EnlaceInvalido();

        // En la base solo está el hash: se busca por el SHA-256 del token recibido.
        var tokenHash = GeneradorTokens.CalcularHash(tokenPlano);

        var token = await _contexto.TokensActivacion
            .Include(t => t.Usuario)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash);

        // Las comprobaciones van antes de cualquier cambio: un rechazo no modifica nada en la base.
        if (token is null)
            return ResultadoActivacion.EnlaceInvalido();

        // Un solo uso (RF-CA-16): abrir el enlace por segunda vez se rechaza.
        if (token.Usado)
            return ResultadoActivacion.EnlaceUsado();

        token.Usuario.Activar();
        token.MarcarUsado();

        // Un solo SaveChanges = una sola transacción: se activa la cuenta y se consume el token, o nada.
        await _contexto.SaveChangesAsync();

        return ResultadoActivacion.Activada();
    }
}
