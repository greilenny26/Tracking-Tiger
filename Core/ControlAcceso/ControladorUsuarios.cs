using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Tracking_Tiger.Core.ControlAcceso;

// Endpoints de administración de usuarios. Solo traducen HTTP ↔ servicio (RD-02).
// Quién puede ejecutar cada acción se declara en CatalogoOperaciones (RF-CA-05), nunca aquí.
[ApiController]
[Route("api/usuarios")]
public sealed class ControladorUsuarios : ControllerBase
{
    private readonly ServicioAdministracionUsuarios _servicio;
    private readonly ServicioRecuperacionContrasena _servicioRecuperacion;

    public ControladorUsuarios(ServicioAdministracionUsuarios servicio, ServicioRecuperacionContrasena servicioRecuperacion)
    {
        _servicio = servicio;
        _servicioRecuperacion = servicioRecuperacion;
    }

    // RF-CA-21: lista [{id, nombre, correo, rol, activo}], ordenada por fecha de creación.
    [Operacion(CatalogoOperaciones.ListarUsuarios)]
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await _servicio.ListarAsync());

    // RF-CA-08: 200 con el usuario actualizado, 400 si el rol no es válido o es el propio usuario,
    // 404 si el id no existe.
    [Operacion(CatalogoOperaciones.CambiarRol)]
    [HttpPut("{id:int}/rol")]
    public async Task<IActionResult> CambiarRol(int id, [FromBody] SolicitudCambioRol solicitud)
    {
        // El filtro ya garantizó una sesión válida; el id sale de esa sesión (claim del servidor).
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idSolicitante))
            return Unauthorized(new { mensaje = "Sesión no válida o vencida." });

        var resultado = await _servicio.CambiarRolAsync(id, solicitud.Rol, idSolicitante);

        return resultado.Estado switch
        {
            EstadoCambioRol.Cambiado => Ok(resultado.Usuario),
            EstadoCambioRol.NoEncontrado => NotFound(new { mensaje = resultado.Mensaje }),
            _ => BadRequest(new { mensaje = resultado.Mensaje })
        };
    }

    // RF-CA-20: 200 con el usuario desactivado, 400 si es la propia cuenta, 404 si el id no existe.
    [Operacion(CatalogoOperaciones.DesactivarUsuario)]
    [HttpPost("{id:int}/desactivar")]
    public async Task<IActionResult> Desactivar(int id)
    {
        // El filtro ya garantizó una sesión válida; el id sale de esa sesión (claim del servidor).
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var idSolicitante))
            return Unauthorized(new { mensaje = "Sesión no válida o vencida." });

        var resultado = await _servicio.DesactivarAsync(id, idSolicitante);

        return resultado.Estado switch
        {
            EstadoDesactivacion.Desactivado => Ok(new { mensaje = resultado.Mensaje, usuario = resultado.Usuario }),
            EstadoDesactivacion.PropioUsuario => BadRequest(new { mensaje = resultado.Mensaje }),
            _ => NotFound(new { mensaje = resultado.Mensaje })
        };
    }

    // RF-CA-20: 200 con el usuario reactivado, 400 si nunca activó su cuenta por correo, 404 si el id no existe.
    [Operacion(CatalogoOperaciones.ReactivarUsuario)]
    [HttpPost("{id:int}/reactivar")]
    public async Task<IActionResult> Reactivar(int id)
    {
        var resultado = await _servicio.ReactivarAsync(id);

        return resultado.Estado switch
        {
            EstadoReactivacion.Reactivado => Ok(new { mensaje = resultado.Mensaje, usuario = resultado.Usuario }),
            EstadoReactivacion.SinActivarPorCorreo => BadRequest(new { mensaje = resultado.Mensaje }),
            _ => NotFound(new { mensaje = resultado.Mensaje })
        };
    }

    // RF-CA-13: 200 si se forzó el restablecimiento, 404 si el id no existe.
    [Operacion(CatalogoOperaciones.ForzarRestablecimiento)]
    [HttpPost("{id:int}/forzar-restablecimiento")]
    public async Task<IActionResult> ForzarRestablecimiento(int id)
    {
        var resultado = await _servicioRecuperacion.ForzarRestablecimientoAsync(id);

        return resultado.Estado == EstadoForzarRestablecimiento.Forzado
            ? Ok(new { mensaje = resultado.Mensaje, usuario = resultado.Usuario })
            : NotFound(new { mensaje = resultado.Mensaje });
    }
}
