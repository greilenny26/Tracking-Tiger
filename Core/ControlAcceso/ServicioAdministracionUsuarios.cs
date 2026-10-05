using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.ControlAcceso;

// Operaciones del Administrador sobre los usuarios (RF-CA-21, RF-CA-08, RF-CA-20). Quién puede ejecutarlas
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

    // RF-CA-08: cambia el rol de un usuario. Surte efecto en la siguiente petición de ese usuario,
    // porque FiltroAutorizacionOperaciones lee el rol de la base en cada petición.
    public async Task<ResultadoCambioRol> CambiarRolAsync(int usuarioId, string? rolSolicitado)
    {
        if (string.IsNullOrWhiteSpace(rolSolicitado))
            return ResultadoCambioRol.RolInvalido("El rol es obligatorio.");

        // Solo los nombres declarados en Rol (sin distinguir mayúsculas). No se aceptan números
        // ("0", "1") ni valores fuera del enum, que Enum.TryParse sí aceptaría.
        var nombre = Enum.GetNames<Rol>()
            .FirstOrDefault(n => n.Equals(rolSolicitado.Trim(), StringComparison.OrdinalIgnoreCase));
        if (nombre is null)
            return ResultadoCambioRol.RolInvalido(
                $"El rol no es válido. Valores permitidos: {string.Join(", ", Enum.GetNames<Rol>())}.");

        var usuario = await _contexto.Usuarios.SingleOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario is null)
            return ResultadoCambioRol.NoEncontrado();

        usuario.CambiarRol(Enum.Parse<Rol>(nombre));
        await _contexto.SaveChangesAsync();

        return ResultadoCambioRol.Cambiado(new UsuarioListado(
            usuario.Id, usuario.Nombre, usuario.Correo, usuario.Rol.ToString(), usuario.Activo));
    }

    // RF-CA-20: desactiva al usuario y revoca TODAS sus sesiones abiertas en una sola transacción:
    // o quedan ambas cosas o ninguna. Desde la siguiente petición sus credenciales dejan de servir
    // (el esquema de sesión rechaza sesiones revocadas y usuarios inactivos) y no puede iniciar sesión.
    public async Task<ResultadoDesactivacion> DesactivarAsync(int usuarioId, int idSolicitante)
    {
        // RF-CA-20: el id de quien pide la operación viene de la sesión del servidor, nunca del cliente.
        // Se compara antes de tocar nada: si es la propia cuenta no se abre la transacción ni se revoca nada.
        if (usuarioId == idSolicitante)
            return ResultadoDesactivacion.PropioUsuario();

        await using var transaccion = await _contexto.Database.BeginTransactionAsync();

        var usuario = await _contexto.Usuarios.SingleOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario is null)
            return ResultadoDesactivacion.NoEncontrado();

        usuario.Desactivar();
        await _contexto.SaveChangesAsync();

        await _contexto.SesionesUsuario
            .Where(s => s.UsuarioId == usuarioId && !s.Revocada)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Revocada, true));

        await transaccion.CommitAsync();

        return ResultadoDesactivacion.Desactivado(new UsuarioListado(
            usuario.Id, usuario.Nombre, usuario.Correo, usuario.Rol.ToString(), usuario.Activo));
    }

    // RF-CA-20: reactiva al usuario. Las sesiones revocadas al desactivarlo siguen revocadas
    // (el esquema de sesión las rechaza siempre); para entrar debe iniciar sesión de nuevo.
    public async Task<ResultadoReactivacion> ReactivarAsync(int usuarioId)
    {
        var usuario = await _contexto.Usuarios.SingleOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario is null)
            return ResultadoReactivacion.NoEncontrado();

        // RF-CA-20: si nunca activó su cuenta por correo, reactivar no la activa (no se salta ese paso).
        if (!usuario.Activo && !usuario.Desactivado)
            return ResultadoReactivacion.SinActivarPorCorreo();

        usuario.Reactivar();
        await _contexto.SaveChangesAsync();

        return ResultadoReactivacion.Reactivado(new UsuarioListado(
            usuario.Id, usuario.Nombre, usuario.Correo, usuario.Rol.ToString(), usuario.Activo));
    }
}
