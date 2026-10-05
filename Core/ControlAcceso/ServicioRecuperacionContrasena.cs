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
    private readonly IHasherContrasenas _hasher;
    private readonly ILogger<ServicioRecuperacionContrasena> _registro;

    public ServicioRecuperacionContrasena(ContextoDatos contexto, IReloj reloj, IColaCorreo cola,
        IHasherContrasenas hasher, ILogger<ServicioRecuperacionContrasena> registro)
    {
        _contexto = contexto;
        _reloj = reloj;
        _cola = cola;
        _hasher = hasher;
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

    // RF-CA-11: con un código válido define la contraseña nueva. Un código desconocido, ya usado o
    // vencido se rechaza con el mismo mensaje y sin cambiar nada. RF-CA-12: al restablecerla se
    // revocan TODAS las sesiones del usuario. RF-CA-14: la contraseña nueva cumple la política antes de
    // tocar el código; si no la cumple, el código sigue sin usar y el usuario puede reintentar.
    public async Task<ResultadoRestablecimiento> RestablecerAsync(string? codigo, string? nuevaContrasena)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return ResultadoRestablecimiento.CodigoInvalido();
        if (string.IsNullOrEmpty(nuevaContrasena))
            return ResultadoRestablecimiento.DatosInvalidos("La contraseña nueva es obligatoria.");

        // RF-CA-14: misma política que en el registro (PoliticaContrasena, único lugar de la regla).
        // Se valida antes de buscar el código: un rechazo no lo consume.
        var validacionContrasena = PoliticaContrasena.Validar(nuevaContrasena);
        if (!validacionContrasena.Valido)
            return ResultadoRestablecimiento.DatosInvalidos(validacionContrasena.Mensaje!);

        // En la base solo está el hash: se busca por el SHA-256 del código recibido.
        var codigoHash = GeneradorTokens.CalcularHash(codigo.Trim());
        var registro = await _contexto.CodigosRecuperacion
            .Include(c => c.Usuario)
            .SingleOrDefaultAsync(c => c.CodigoHash == codigoHash);

        // RF-CA-10: un solo uso y con vencimiento (reloj único en UTC, RD-11). Las comprobaciones van
        // antes de cualquier cambio: un rechazo no modifica nada.
        if (registro is null || registro.Usado || registro.FechaVencimiento < _reloj.AhoraUtc)
            return ResultadoRestablecimiento.CodigoInvalido();

        // Contraseña nueva, código usado y sesiones revocadas en UNA transacción: o todo o nada.
        await using var transaccion = await _contexto.Database.BeginTransactionAsync();

        registro.Usuario.CambiarContrasena(_hasher.Hashear(nuevaContrasena));
        registro.MarcarUsado();
        await _contexto.SaveChangesAsync();

        // RF-CA-12: toda credencial emitida antes del restablecimiento deja de servir.
        await _contexto.SesionesUsuario
            .Where(s => s.UsuarioId == registro.UsuarioId && !s.Revocada)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Revocada, true));

        await transaccion.CommitAsync();

        return ResultadoRestablecimiento.Restablecida();
    }
}
