using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.ControlAcceso;

// Recuperación de contraseña (RF-CA-09, RF-CA-10). Toda la regla vive aquí, no en el controlador (RD-02).
// Un correo mal formado se rechaza (no revela nada); cualquier correo bien formado recibe la misma
// respuesta, exista o no. Genera el código pero todavía no lo envía. Nunca se registran correos ni códigos.
public sealed class ServicioRecuperacionContrasena
{
    private readonly ContextoDatos _contexto;
    private readonly IReloj _reloj;

    public ServicioRecuperacionContrasena(ContextoDatos contexto, IReloj reloj)
    {
        _contexto = contexto;
        _reloj = reloj;
    }

    public async Task<ResultadoSolicitudRecuperacion> SolicitarAsync(string? correo)
    {
        var validacion = ValidadorCorreo.Validar(correo);
        if (!validacion.Valido)
            return ResultadoSolicitudRecuperacion.DatosInvalidos(validacion.Mensaje!);

        // El código se prepara siempre, exista o no el usuario, y no hay retorno anticipado:
        // todos los casos terminan en la misma respuesta (RF-CA-09).
        // El valor en claro solo vive en esta variable; en la base queda únicamente su SHA-256.
        // Nunca se registra en logs.
        var codigoPlano = GeneradorTokens.GenerarToken();
        var codigoHash = GeneradorTokens.CalcularHash(codigoPlano);

        var correoNormalizado = Usuario.NormalizarCorreo(correo!);
        var usuario = await _contexto.Usuarios.SingleOrDefaultAsync(u => u.Correo == correoNormalizado);

        // Solo un usuario existente y ACTIVO recibe código.
        if (usuario is { Activo: true })
        {
            // RF-CA-10: los códigos anteriores sin usar dejan de servir.
            var anteriores = await _contexto.CodigosRecuperacion
                .Where(c => c.UsuarioId == usuario.Id && !c.Usado)
                .ToListAsync();
            foreach (var anterior in anteriores)
                anterior.MarcarUsado();

            _contexto.CodigosRecuperacion.Add(CodigoRecuperacion.Emitir(usuario, codigoHash, _reloj.AhoraUtc));

            // Un solo SaveChanges = una sola transacción: códigos anteriores marcados y código nuevo, o nada.
            await _contexto.SaveChangesAsync();
        }

        return ResultadoSolicitudRecuperacion.Aceptada();
    }
}
