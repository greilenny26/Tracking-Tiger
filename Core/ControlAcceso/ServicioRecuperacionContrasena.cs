namespace Tracking_Tiger.Core.ControlAcceso;

// Recuperación de contraseña (RF-CA-09). Toda la regla vive aquí, no en el controlador (RD-02).
// Un correo mal formado se rechaza (no revela nada); cualquier correo bien formado recibe la misma
// respuesta, exista o no. Todavía no se genera ni se envía ningún código. Nunca se registran correos.
public sealed class ServicioRecuperacionContrasena
{
    public Task<ResultadoSolicitudRecuperacion> SolicitarAsync(string? correo)
    {
        var validacion = ValidadorCorreo.Validar(correo);
        if (!validacion.Valido)
            return Task.FromResult(ResultadoSolicitudRecuperacion.DatosInvalidos(validacion.Mensaje!));

        return Task.FromResult(ResultadoSolicitudRecuperacion.Aceptada());
    }
}
