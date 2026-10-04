using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.ControlAcceso;

// Inicio de sesión (RF-CA-03). Toda la regla vive aquí, no en el controlador (RD-02).
// La comprobación de cuenta activa llega en un paso aparte.
public sealed class ServicioSesion
{
    // Hash ficticio con el mismo algoritmo e iteraciones que los reales. Se calcula una sola vez por
    // proceso a partir de un valor aleatorio: ninguna contraseña real coincide con él.
    private static string? _hashFicticio;

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

        // RF-CA-03: correo inexistente y contraseña incorrecta deben ser indistinguibles. Si el usuario
        // no existe se verifica igual contra el hash ficticio, así ambos casos hacen el mismo trabajo
        // (una consulta + un PBKDF2) y tardan lo mismo. Nunca se registran correos ni contraseñas.
        var hashAComparar = usuario?.HashContrasena ?? _hashFicticio!;
        var contrasenaCorrecta = _hasher.Verificar(contrasena, hashAComparar);

        if (usuario is null || !contrasenaCorrecta)
            return ResultadoInicioSesion.CredencialesInvalidas();

        // Credencial de sesión: 32 bytes aleatorios en Base64 apto para URL. En la base solo queda
        // su SHA-256; el valor en claro solo viaja en esta respuesta y nunca se registra en logs.
        var tokenPlano = GeneradorTokens.GenerarToken();
        var sesion = SesionUsuario.Emitir(usuario, GeneradorTokens.CalcularHash(tokenPlano), _reloj.AhoraUtc);

        _contexto.SesionesUsuario.Add(sesion);
        await _contexto.SaveChangesAsync();

        return ResultadoInicioSesion.Iniciada(tokenPlano, sesion.FechaVencimiento);
    }
}
