using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.ControlAcceso;

// Operaciones del Administrador sobre los usuarios (RF-CA-21). Quién puede ejecutarlas
// lo decide CatalogoOperaciones; aquí solo vive la lógica (RD-02).
public sealed class ServicioAdministracionUsuarios
{
    private readonly ContextoDatos _contexto;

    public ServicioAdministracionUsuarios(ContextoDatos contexto)
    {
        _contexto = contexto;
    }

    // Listado ordenado por fecha de creación (el Id desempata). La consulta solo lee las cinco
    // columnas públicas: los campos internos ni siquiera se cargan de la base.
    public async Task<IReadOnlyList<UsuarioListado>> ListarAsync()
    {
        var filas = await _contexto.Usuarios
            .AsNoTracking()
            .OrderBy(u => u.FechaCreacion)
            .ThenBy(u => u.Id)
            .Select(u => new { u.Id, u.Nombre, u.Correo, u.Rol, u.Activo })
            .ToListAsync();

        return filas
            .Select(f => new UsuarioListado(f.Id, f.Nombre, f.Correo, f.Rol.ToString(), f.Activo))
            .ToList();
    }
}
