using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.Correo;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.ControlAcceso;

// Recuperación de contraseña (RF-CA-09, RF-CA-10). Toda la regla vive aquí, no en el controlador (RD-02).
// Un correo mal formado se rechaza (no revela nada); cualquier correo bien formado recibe la misma
// respuesta, exista o no. Genera el código y encola el correo con él. Nunca se registran correos ni códigos.
public sealed class ServicioRecuperacionContrasena
{
    private readonly ContextoDatos _contexto;
    private readonly IReloj _reloj;
    private readonly IColaCorreo _cola;
    private readonly ILogger<ServicioRecuperacionContrasena> _registro;

    public ServicioRecuperacionContrasena(ContextoDatos contexto, IReloj reloj, IColaCorreo cola,
        ILogger<ServicioRecuperacionContrasena> registro)
    {
        _contexto = contexto;
        _reloj = reloj;
        _cola = cola;
        _registro = registro;
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

            // El correo solo se registra en la cola (RF-NOT-08): nunca se contacta a SMTP aquí y la
            // operación termina bien aunque el servidor de correo esté caído. Lo envía "enviar-correos".
            var encolado = _cola.Agregar(usuario.Correo, PlantillaCorreoRecuperacion.Asunto,
                PlantillaCorreoRecuperacion.Cuerpo(usuario.Nombre, codigoPlano));

            if (encolado.Exito)
            {
                // Un solo SaveChanges = una sola transacción: códigos anteriores marcados, código nuevo
                // y correo en la cola, o nada.
                await _contexto.SaveChangesAsync();
            }
            else
            {
                // Solo queda en el log del servidor (sin código ni correo); al cliente le llega la respuesta de siempre.
                _contexto.ChangeTracker.Clear();
                _registro.LogError("No se pudo encolar el correo de recuperación: {Motivo}", encolado.Mensaje);
            }
        }

        return ResultadoSolicitudRecuperacion.Aceptada();
    }
}
