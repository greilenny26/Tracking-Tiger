using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.Correo;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.ControlAcceso;

// Registro de usuarios con correo único (RF-CA-01). Toda la regla vive aquí, no en el controlador (RD-02).
// La cuenta nace inactiva (Usuario.CrearNuevo) y el correo de activación queda en la cola (RF-CA-15).
public sealed class ServicioRegistro
{
    // Debe coincidir con el límite declarado en ContextoDatos.
    private const int LargoMaximoNombre = 100;

    // Código extendido de SQLite para la violación de un índice único (SQLITE_CONSTRAINT_UNIQUE).
    private const int ErrorSqliteRestriccionUnica = 2067;

    private readonly ContextoDatos _contexto;
    private readonly IHasherContrasenas _hasher;
    private readonly IReloj _reloj;
    private readonly IColaCorreo _cola;
    private readonly ILogger<ServicioRegistro> _registro;

    public ServicioRegistro(ContextoDatos contexto, IHasherContrasenas hasher, IReloj reloj,
        IColaCorreo cola, ILogger<ServicioRegistro> registro)
    {
        _contexto = contexto;
        _hasher = hasher;
        _reloj = reloj;
        _cola = cola;
        _registro = registro;
    }

    public async Task<ResultadoRegistro> RegistrarAsync(string? nombre, string? correo, string? contrasena)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return ResultadoRegistro.DatosInvalidos("El nombre es obligatorio.");

        nombre = nombre.Trim();
        if (nombre.Length > LargoMaximoNombre)
            return ResultadoRegistro.DatosInvalidos($"El nombre no puede tener más de {LargoMaximoNombre} caracteres.");

        var validacionCorreo = ValidadorCorreo.Validar(correo);
        if (!validacionCorreo.Valido)
            return ResultadoRegistro.DatosInvalidos(validacionCorreo.Mensaje!);

        var validacionContrasena = PoliticaContrasena.Validar(contrasena);
        if (!validacionContrasena.Valido)
            return ResultadoRegistro.DatosInvalidos(validacionContrasena.Mensaje!);

        var correoNormalizado = Usuario.NormalizarCorreo(correo!);

        if (await _contexto.Usuarios.AnyAsync(u => u.Correo == correoNormalizado))
            return ResultadoRegistro.CorreoDuplicado();

        // Token de activación (RF-CA-15): en TokensActivacion solo queda su hash. El valor en claro
        // solo viaja dentro del enlace del correo y nunca se registra en logs.
        var tokenPlano = GeneradorTokens.GenerarToken();

        // Sin APP_URL_BASE no se puede armar el enlace: no se guarda nada y la respuesta es controlada.
        var enlace = EnlaceActivacion.Construir(tokenPlano);
        if (!enlace.Exito)
        {
            _registro.LogError("No se pudo registrar al usuario: {Motivo}", enlace.Mensaje);
            return ResultadoRegistro.NoDisponible();
        }

        // Nace inactivo por regla de dominio (RF-CA-15).
        var ahora = _reloj.AhoraUtc;
        var usuario = Usuario.CrearNuevo(nombre, correoNormalizado, _hasher.Hashear(contrasena!), ahora);
        var token = TokenActivacion.Emitir(usuario, GeneradorTokens.CalcularHash(tokenPlano), ahora);

        _contexto.Usuarios.Add(usuario);
        _contexto.TokensActivacion.Add(token);

        // El correo solo se registra en la cola (RF-NOT-08): el registro nunca contacta al servidor SMTP
        // y termina bien aunque esté caído. Lo envía después el proceso "enviar-correos".
        var encolado = _cola.Agregar(usuario.Correo, PlantillaCorreoActivacion.Asunto,
            PlantillaCorreoActivacion.Cuerpo(usuario.Nombre, enlace.Enlace!));
        if (!encolado.Exito)
        {
            _contexto.ChangeTracker.Clear();
            _registro.LogError("No se pudo encolar el correo de activación: {Motivo}", encolado.Mensaje);
            return ResultadoRegistro.NoDisponible();
        }

        try
        {
            // Un solo SaveChanges = una sola transacción: usuario, token y correo se guardan juntos, o ninguno.
            await _contexto.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (EsCorreoDuplicado(error))
        {
            // Carrera: otra petición registró el mismo correo entre la consulta y el guardado.
            // El índice único lo impidió; se responde igual que en la comprobación previa.
            _contexto.ChangeTracker.Clear();
            return ResultadoRegistro.CorreoDuplicado();
        }

        return ResultadoRegistro.Registrado(
            new UsuarioRegistrado(usuario.Id, usuario.Nombre, usuario.Correo, usuario.Activo));
    }

    // Solo la violación del índice único de Usuarios.Correo; cualquier otra sigue al manejador global.
    private static bool EsCorreoDuplicado(DbUpdateException error) =>
        error.InnerException is SqliteException sqlite
        && sqlite.SqliteExtendedErrorCode == ErrorSqliteRestriccionUnica
        && sqlite.Message.Contains("Usuarios.Correo", StringComparison.Ordinal);
}
