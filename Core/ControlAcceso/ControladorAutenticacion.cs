using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Tracking_Tiger.Core.ControlAcceso;

// Endpoints de autenticación. Solo traducen HTTP ↔ servicio; las reglas viven en los servicios (RD-02).
// Quién puede ejecutar cada acción se declara en CatalogoOperaciones (RF-CA-05), nunca aquí.
[ApiController]
[Route("api/auth")]
public sealed class ControladorAutenticacion : ControllerBase
{
    private readonly ServicioRegistro _servicioRegistro;
    private readonly ServicioActivacion _servicioActivacion;
    private readonly ServicioReenvioActivacion _servicioReenvio;
    private readonly ServicioSesion _servicioSesion;
    private readonly ServicioRecuperacionContrasena _servicioRecuperacion;

    public ControladorAutenticacion(ServicioRegistro servicioRegistro, ServicioActivacion servicioActivacion,
        ServicioReenvioActivacion servicioReenvio, ServicioSesion servicioSesion,
        ServicioRecuperacionContrasena servicioRecuperacion)
    {
        _servicioRegistro = servicioRegistro;
        _servicioActivacion = servicioActivacion;
        _servicioReenvio = servicioReenvio;
        _servicioSesion = servicioSesion;
        _servicioRecuperacion = servicioRecuperacion;
    }

    // RF-CA-01: 201 con los datos públicos del usuario, 400 si los datos no son válidos,
    // 409 si el correo ya está registrado, 503 si falta configuración del servidor.
    // La respuesta nunca incluye el hash ni el token de activación.
    [Operacion(CatalogoOperaciones.Registro)]
    [HttpPost("registro")]
    public async Task<IActionResult> Registrar([FromBody] SolicitudRegistro solicitud)
    {
        var resultado = await _servicioRegistro.RegistrarAsync(solicitud.Nombre, solicitud.Correo, solicitud.Contrasena);

        return resultado.Estado switch
        {
            EstadoRegistro.Registrado => StatusCode(StatusCodes.Status201Created, new
            {
                mensaje = resultado.Mensaje,
                id = resultado.Usuario!.Id,
                nombre = resultado.Usuario.Nombre,
                correo = resultado.Usuario.Correo,
                activo = resultado.Usuario.Activo
            }),
            EstadoRegistro.CorreoDuplicado => Conflict(new { mensaje = resultado.Mensaje }),
            EstadoRegistro.NoDisponible => StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensaje = resultado.Mensaje }),
            _ => BadRequest(new { mensaje = resultado.Mensaje })
        };
    }

    // RF-CA-16: 200 si la cuenta se activó, 400 con mensaje genérico si el enlace no es válido.
    [Operacion(CatalogoOperaciones.Activar)]
    [HttpGet("activar")]
    public async Task<IActionResult> Activar([FromQuery] string? token)
    {
        var resultado = await _servicioActivacion.ActivarAsync(token);

        return resultado.Estado == EstadoActivacion.Activada
            ? Ok(new { mensaje = resultado.Mensaje })
            : BadRequest(new { mensaje = resultado.Mensaje });
    }

    // RF-CA-17: 200 con la misma respuesta exista o no el correo, 400 solo si el correo no tiene
    // formato válido, 503 si falta configuración del servidor.
    [Operacion(CatalogoOperaciones.ReenviarActivacion)]
    [HttpPost("reenviar-activacion")]
    public async Task<IActionResult> ReenviarActivacion([FromBody] SolicitudReenvioActivacion solicitud)
    {
        var resultado = await _servicioReenvio.ReenviarAsync(solicitud.Correo);

        return resultado.Estado switch
        {
            EstadoReenvio.Aceptado => Ok(new { mensaje = resultado.Mensaje }),
            EstadoReenvio.NoDisponible => StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensaje = resultado.Mensaje }),
            _ => BadRequest(new { mensaje = resultado.Mensaje })
        };
    }

    // RF-CA-03: 200 con la credencial de sesión y su vencimiento (UTC), 400 si falta el correo
    // o la contraseña, 403 si la contraseña es correcta pero la cuenta no está activa (RF-CA-15),
    // 429 mientras la cuenta está bloqueada por intentos fallidos (RF-CA-19),
    // 401 con el mismo mensaje ante cualquier otra falla.
    // El token en claro solo aparece en esta respuesta.
    [Operacion(CatalogoOperaciones.IniciarSesion)]
    [HttpPost("login")]
    public async Task<IActionResult> IniciarSesion([FromBody] SolicitudInicioSesion solicitud)
    {
        var resultado = await _servicioSesion.IniciarSesionAsync(solicitud.Correo, solicitud.Contrasena);

        return resultado.Estado switch
        {
            EstadoInicioSesion.Iniciada => Ok(new { token = resultado.Token, venceEn = resultado.VenceEn }),
            EstadoInicioSesion.DatosInvalidos => BadRequest(new { mensaje = resultado.Mensaje }),
            EstadoInicioSesion.CuentaInactiva => StatusCode(StatusCodes.Status403Forbidden, new { mensaje = resultado.Mensaje }),
            EstadoInicioSesion.Bloqueada => StatusCode(StatusCodes.Status429TooManyRequests, new { mensaje = resultado.Mensaje }),
            _ => Unauthorized(new { mensaje = resultado.Mensaje })
        };
    }

    // RF-CA-07: datos del usuario autenticado. Sin credencial válida, el esquema de sesión responde 401.
    [Operacion(CatalogoOperaciones.ConsultarUsuarioActual)]
    [HttpGet("yo")]
    public async Task<IActionResult> ObtenerUsuarioActual()
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId))
            return Unauthorized(new { mensaje = "Sesión no válida o vencida." });

        var usuario = await _servicioSesion.ObtenerUsuarioActualAsync(usuarioId);
        return usuario is null
            ? Unauthorized(new { mensaje = "Sesión no válida o vencida." })
            : Ok(new { id = usuario.Id, nombre = usuario.Nombre, correo = usuario.Correo, rol = usuario.Rol.ToString() });
    }

    // RF-CA-18: cierra la sesión actual. Sin sesión válida, el esquema responde 401 antes de llegar aquí.
    [Operacion(CatalogoOperaciones.CerrarSesion)]
    [HttpPost("logout")]
    public async Task<IActionResult> CerrarSesion()
    {
        if (!int.TryParse(User.FindFirstValue(ManejadorAutenticacionSesion.ClaimSesionId), out var sesionId))
            return Unauthorized(new { mensaje = "Sesión no válida o vencida." });

        await _servicioSesion.CerrarSesionAsync(sesionId);
        return Ok(new { mensaje = "Sesión cerrada correctamente." });
    }

    // RF-CA-09: 200 con la misma respuesta para cualquier correo bien formado; 400 solo si el formato no es válido.
    [Operacion(CatalogoOperaciones.SolicitarRecuperacion)]
    [HttpPost("recuperar")]
    public async Task<IActionResult> SolicitarRecuperacion([FromBody] SolicitudRecuperacion solicitud)
    {
        var resultado = await _servicioRecuperacion.SolicitarAsync(solicitud.Correo);

        return resultado.Estado == EstadoSolicitudRecuperacion.Aceptada
            ? Ok(new { mensaje = resultado.Mensaje })
            : BadRequest(new { mensaje = resultado.Mensaje });
    }
}
