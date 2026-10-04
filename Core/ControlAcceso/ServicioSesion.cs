using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.ControlAcceso;

// Inicio de sesión (RF-CA-03). Toda la regla vive aquí, no en el controlador (RD-02).
// Una cuenta sin activar solo se rechaza como tal después de verificar la contraseña (RF-CA-15).
// Un inicio de sesión correcto reinicia los intentos fallidos (RF-CA-19).
public sealed class ServicioSesion
{
    // Hash ficticio con el mismo algoritmo e iteraciones que los reales. Se calcula una sola vez por
    // proceso a partir de un valor aleatorio: ninguna contraseña real coincide con él.
    private static string? _hashFicticio;

    // RF-CA-19: al 5.º fallo la cuenta queda bloqueada 15 minutos.
    private const int LimiteIntentosFallidos = 5;
    private static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    private readonly ContextoDatos _contexto;
    private readonly IHasherContrasenas _hasher;
    private readonly IReloj _reloj;

    public ServicioSesion(ContextoDatos contexto, IHasherContrasenas hasher, IReloj reloj)
    {
        _contexto = contexto;
        _hasher = hasher;
        _reloj = reloj;
        LazyInitializer.EnsureInitialized(ref _hashFicticio, () => _hasher.Hashear(GeneradorTokens.GenerarToken()));
    }

    public async Task<ResultadoInicioSesion> IniciarSesionAsync(string? correo, string? contrasena)
    {
        if (string.IsNullOrWhiteSpace(correo))
            return ResultadoInicioSesion.DatosInvalidos("El correo es obligatorio.");
        if (string.IsNullOrEmpty(contrasena))
            return ResultadoInicioSesion.DatosInvalidos("La contraseña es obligatoria.");

        var correoNormalizado = Usuario.NormalizarCorreo(correo);
        var usuario = await _contexto.Usuarios.SingleOrDefaultAsync(u => u.Correo == correoNormalizado);
        var ahora = _reloj.AhoraUtc;

        if (usuario?.BloqueadoHasta is { } bloqueadoHasta)
        {
            // RF-CA-19: mientras dure el bloqueo se rechaza TODO intento, sin verificar la contraseña,
            // sin tocar el contador y sin extender el bloqueo.
            if (bloqueadoHasta > ahora)
                return ResultadoInicioSesion.Bloqueada();

            // Bloqueo vencido: contador a 0 y sin bloqueo antes de procesar este intento.
            await _contexto.Usuarios
                .Where(u => u.Id == usuario.Id && u.BloqueadoHasta != null && u.BloqueadoHasta <= ahora)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.IntentosFallidos, 0)
                    .SetProperty(u => u.BloqueadoHasta, (DateTime?)null));
        }

        // RF-CA-03: correo inexistente y contraseña incorrecta deben ser indistinguibles. Si el usuario
        // no existe se verifica igual contra el hash ficticio, así ambos casos hacen el mismo trabajo
        // (una consulta + un PBKDF2) y tardan lo mismo. Nunca se registran correos ni contraseñas.
        var hashAComparar = usuario?.HashContrasena ?? _hashFicticio!;
        var contrasenaCorrecta = _hasher.Verificar(contrasena, hashAComparar);

        if (usuario is null || !contrasenaCorrecta)
        {
            // RF-CA-19: una contraseña incorrecta de un usuario EXISTENTE suma un intento fallido.
            // Un correo inexistente no cambia nada. La respuesta es la misma en ambos casos.
            if (usuario is not null)
            {
                var hasta = ahora + DuracionBloqueo;
                await _contexto.Usuarios
                    .Where(u => u.Id == usuario.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(u => u.IntentosFallidos, u => u.IntentosFallidos + 1)
                        // Al llegar al 5.º fallo se bloquea; si ya hay bloqueo, no se extiende.
                        .SetProperty(u => u.BloqueadoHasta, u =>
                            u.IntentosFallidos + 1 >= LimiteIntentosFallidos && u.BloqueadoHasta == null
                                ? hasta
                                : u.BloqueadoHasta));
            }

            return ResultadoInicioSesion.CredencialesInvalidas();
        }

        // RF-CA-15: la cuenta sin activar se informa SOLO cuando la contraseña ya es correcta. Con una
        // contraseña incorrecta sale el 401 genérico de arriba, así quien no conoce la contraseña
        // no puede averiguar el estado de una cuenta.
        if (!usuario.Activo)
            return ResultadoInicioSesion.CuentaInactiva();

        // Credencial de sesión: 32 bytes aleatorios en Base64 apto para URL. En la base solo queda
        // su SHA-256; el valor en claro solo viaja en esta respuesta y nunca se registra en logs.
        var tokenPlano = GeneradorTokens.GenerarToken();
        var sesion = SesionUsuario.Emitir(usuario, GeneradorTokens.CalcularHash(tokenPlano), _reloj.AhoraUtc);

        // RF-CA-19: un inicio de sesión correcto pone el contador en cero y quita cualquier bloqueo.
        // Se hace en la misma transacción que la sesión nueva: o quedan ambas cosas o ninguna.
        await using var transaccion = await _contexto.Database.BeginTransactionAsync();

        await _contexto.Usuarios
            .Where(u => u.Id == usuario.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.IntentosFallidos, 0)
                .SetProperty(u => u.BloqueadoHasta, (DateTime?)null));

        _contexto.SesionesUsuario.Add(sesion);
        await _contexto.SaveChangesAsync();

        await transaccion.CommitAsync();

        return ResultadoInicioSesion.Iniciada(tokenPlano, sesion.FechaVencimiento);
    }

    // Cierre de sesión (RF-CA-18): revoca solo la sesión indicada; las demás sesiones del usuario siguen válidas.
    // No valida la sesión: eso ya lo hizo el esquema de autenticación antes de llegar aquí.
    public async Task CerrarSesionAsync(int sesionId)
    {
        var sesion = await _contexto.SesionesUsuario.SingleOrDefaultAsync(s => s.Id == sesionId);
        if (sesion is null)
            return;

        sesion.Revocar();
        await _contexto.SaveChangesAsync();
    }

    // Datos públicos del usuario autenticado (RF-CA-07). Null si el usuario ya no existe.
    public async Task<UsuarioActual?> ObtenerUsuarioActualAsync(int usuarioId) =>
        await _contexto.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == usuarioId)
            .Select(u => new UsuarioActual(u.Id, u.Nombre, u.Correo))
            .SingleOrDefaultAsync();
}
