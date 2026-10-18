using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.ControlAcceso;

// Reenvío del enlace de activación (RF-CA-17). Toda la regla vive aquí, no en el controlador (RD-02).
// Solo un usuario existente y todavía inactivo recibe un token nuevo y un correo nuevo, pero la
// respuesta es idéntica exista o no el correo, o esté ya activa la cuenta: no revela qué correos existen.
// Solo un formato de correo inválido (no revela nada) o falta de configuración responden distinto.
public sealed class ServicioReenvioActivacion
{
    private readonly ContextoDatos _contexto;
    private readonly EmisorActivacion _emisor;
    private readonly IReloj _reloj;
    private readonly ILogger<ServicioReenvioActivacion> _registro;

    public ServicioReenvioActivacion(ContextoDatos contexto, EmisorActivacion emisor, IReloj reloj,
        ILogger<ServicioReenvioActivacion> registro)
    {
        _contexto = contexto;
        _emisor = emisor;
        _reloj = reloj;
        _registro = registro;
    }

    public async Task<ResultadoReenvio> ReenviarAsync(string? correo)
    {
        var validacionCorreo = ValidadorCorreo.Validar(correo);
        if (!validacionCorreo.Valido)
            return ResultadoReenvio.DatosInvalidos(validacionCorreo.Mensaje!);

        var correoNormalizado = Usuario.NormalizarCorreo(correo!);

        // El enlace se prepara antes de buscar al usuario: si falta APP_URL_BASE, la respuesta
        // no depende de que el correo exista.
        var enlace = EmisorActivacion.Preparar(out var errorEnlace);
        if (enlace is null)
        {
            _registro.LogError("No se pudo reenviar la activación: {Motivo}", errorEnlace);
            return ResultadoReenvio.NoDisponible();
        }

        // La búsqueda se hace siempre, exista o no el correo, y no hay retorno anticipado:
        // todos los casos terminan en la misma respuesta (RF-CA-17).
        var usuario = await _contexto.Usuarios.SingleOrDefaultAsync(u => u.Correo == correoNormalizado);
        // RF-CA-20: una cuenta desactivada por un Administrador no recibe enlace (no puede reactivarse sola).
        var debeReenviar = usuario is { Activo: false, Desactivado: false };

        if (debeReenviar)
        {
            var encolado = await _emisor.EmitirAsync(usuario!, enlace, _reloj.AhoraUtc);
            if (encolado.Exito)
            {
                // Un solo SaveChanges = una sola transacción: tokens anteriores invalidados, token nuevo
                // y correo se guardan juntos, o nada.
                await _contexto.SaveChangesAsync();
            }
            else
            {
                // Solo queda en el log del servidor; al cliente le llega la respuesta de siempre.
                _contexto.ChangeTracker.Clear();
                _registro.LogError("No se pudo encolar el correo de activación: {Motivo}", encolado.Mensaje);
            }
        }

        return ResultadoReenvio.Aceptado();
    }
}
