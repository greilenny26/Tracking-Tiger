using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.ControlAcceso;

// Inicio de sesión (RF-CA-03). Toda la regla vive aquí, no en el controlador (RD-02).
// Por ahora solo el camino feliz: la comprobación de cuenta activa y la igualación de tiempos
// entre correo inexistente y contraseña incorrecta llegan en pasos aparte.
public sealed class ServicioSesion
{
    private readonly ContextoDatos _contexto;
    private readonly IHasherContrasenas _hasher;
    private readonly IReloj _reloj;

    public ServicioSesion(ContextoDatos contexto, IHasherContrasenas hasher, IReloj reloj)
    {
        _contexto = contexto;
        _hasher = hasher;
        _reloj = reloj;
    }

    public async Task<ResultadoInicioSesion> IniciarSesionAsync(string? correo, string? contrasena)
    {
        if (string.IsNullOrWhiteSpace(correo))
            return ResultadoInicioSesion.DatosInvalidos("El correo es obligatorio.");
        if (string.IsNullOrEmpty(contrasena))
            return ResultadoInicioSesion.DatosInvalidos("La contraseña es obligatoria.");

        var correoNormalizado = Usuario.NormalizarCorreo(correo);
        var usuario = await _contexto.Usuarios.SingleOrDefaultAsync(u => u.Correo == correoNormalizado);

        if (usuario is null || !_hasher.Verificar(contrasena, usuario.HashContrasena))
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
