using Microsoft.AspNetCore.Mvc;

namespace Tracking_Tiger.Core.ControlAcceso;

// Endpoints de administración de usuarios. Solo traducen HTTP ↔ servicio (RD-02).
// Quién puede ejecutar cada acción se declara en CatalogoOperaciones (RF-CA-05), nunca aquí.
[ApiController]
[Route("api/usuarios")]
public sealed class ControladorUsuarios : ControllerBase
{
    private readonly ServicioAdministracionUsuarios _servicio;

    public ControladorUsuarios(ServicioAdministracionUsuarios servicio)
    {
        _servicio = servicio;
    }

    // RF-CA-21: lista [{id, nombre, correo, rol, activo}], ordenada por fecha de creación.
    [Operacion(CatalogoOperaciones.ListarUsuarios)]
    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await _servicio.ListarAsync());

    // RF-CA-08: 200 con el usuario actualizado, 400 si el rol no es válido, 404 si el id no existe.
    [Operacion(CatalogoOperaciones.CambiarRol)]
    [HttpPut("{id:int}/rol")]
    public async Task<IActionResult> CambiarRol(int id, [FromBody] SolicitudCambioRol solicitud)
    {
        var resultado = await _servicio.CambiarRolAsync(id, solicitud.Rol);

        return resultado.Estado switch
        {
            EstadoCambioRol.Cambiado => Ok(resultado.Usuario),
            EstadoCambioRol.NoEncontrado => NotFound(new { mensaje = resultado.Mensaje }),
            _ => BadRequest(new { mensaje = resultado.Mensaje })
        };
    }
}
