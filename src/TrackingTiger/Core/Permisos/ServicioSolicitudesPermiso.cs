using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.ControlAcceso;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.Permisos;

// Lógica de Gestión de permisos (RD-02). Quién puede ejecutar cada operación lo decide
// CatalogoOperaciones; los ids de usuario llegan siempre de la sesión del servidor, nunca del cliente.
public sealed class ServicioSolicitudesPermiso
{
    // Igual al largo de la columna MotivoRechazo en ContextoDatos.
    private const int LargoMaximoMotivo = 500;

    private readonly ContextoDatos _contexto;
    private readonly IReloj _reloj;

    public ServicioSolicitudesPermiso(ContextoDatos contexto, IReloj reloj)
    {
        _contexto = contexto;
        _reloj = reloj;
    }

    // RF-GP-01: crea la solicitud en Pendiente. Solo se pueden pedir las operaciones declaradas en
    // CatalogoOperaciones.OperacionesSolicitables; cualquier otro valor se rechaza de forma controlada (RD-07).
    public async Task<ResultadoCreacionSolicitud> CrearAsync(int solicitanteId, string? permisoSolicitado)
    {
        if (string.IsNullOrWhiteSpace(permisoSolicitado))
            return ResultadoCreacionSolicitud.PermisoInvalido("El permiso solicitado es obligatorio.");

        var permiso = CatalogoOperaciones.OperacionesSolicitables
            .FirstOrDefault(o => o.Equals(permisoSolicitado.Trim(), StringComparison.OrdinalIgnoreCase));
        if (permiso is null)
            return ResultadoCreacionSolicitud.PermisoInvalido(
                $"Ese permiso no se puede solicitar. Valores permitidos: {string.Join(", ", CatalogoOperaciones.OperacionesSolicitables)}.");

        var yaPendiente = await _contexto.SolicitudesPermiso.AnyAsync(s =>
            s.SolicitanteId == solicitanteId && s.PermisoSolicitado == permiso && s.Estado == EstadoSolicitud.Pendiente);
        if (yaPendiente)
            return ResultadoCreacionSolicitud.YaPendiente();

        var solicitud = SolicitudPermiso.Crear(solicitanteId, permiso, _reloj.AhoraUtc);
        _contexto.SolicitudesPermiso.Add(solicitud);
        await _contexto.SaveChangesAsync();

        var listado = (await ListarAsync(s => s.Id == solicitud.Id)).Single();
        return ResultadoCreacionSolicitud.Creada(listado);
    }

    // RF-GP-04: el Administrador rechaza una solicitud pendiente con un motivo escrito. La máquina de estados
    // decide si se permite (motivo, estado, RF-GP-07); si no, la solicitud no cambia y no se guarda nada.
    public async Task<ResultadoResolucionSolicitud> RechazarAsync(int solicitudId, int administradorId, string? motivo)
    {
        if (motivo is not null && motivo.Trim().Length > LargoMaximoMotivo)
            return ResultadoResolucionSolicitud.DatoInvalido($"El motivo admite como máximo {LargoMaximoMotivo} caracteres.");

        var solicitud = await _contexto.SolicitudesPermiso.SingleOrDefaultAsync(s => s.Id == solicitudId);
        if (solicitud is null)
            return ResultadoResolucionSolicitud.NoEncontrada();

        var transicion = MaquinaEstadosSolicitud.IntentarTransicion(
            solicitud, EstadoSolicitud.Rechazada, administradorId, _reloj.AhoraUtc, motivo);
        if (!transicion.Exito)
            return ResultadoResolucionSolicitud.NoPermitida(transicion.Mensaje);

        await _contexto.SaveChangesAsync();
        return ResultadoResolucionSolicitud.Resuelta((await ListarAsync(s => s.Id == solicitudId)).Single());
    }

    // RF-GP-02: solicitudes en Pendiente de todos los usuarios. Solo el Administrador llega aquí (catálogo).
    public Task<IReadOnlyList<SolicitudPermisoListado>> ListarPendientesAsync() =>
        ListarAsync(s => s.Estado == EstadoSolicitud.Pendiente);

    // RF-GP-02: un usuario solo ve SUS solicitudes; el id sale de su sesión, así no puede ver las de otros.
    public Task<IReadOnlyList<SolicitudPermisoListado>> ListarDeUsuarioAsync(int usuarioId) =>
        ListarAsync(s => s.SolicitanteId == usuarioId);

    private async Task<IReadOnlyList<SolicitudPermisoListado>> ListarAsync(Expression<Func<SolicitudPermiso, bool>> filtro)
    {
        var filas = await _contexto.SolicitudesPermiso
            .AsNoTracking()
            .Where(filtro)
            .Join(_contexto.Usuarios, s => s.SolicitanteId, u => u.Id, (s, u) => new { Solicitud = s, NombreSolicitante = u.Nombre })
            .OrderBy(f => f.Solicitud.FechaCreacion)
            .ThenBy(f => f.Solicitud.Id)
            .ToListAsync();

        return filas
            .Select(f => new SolicitudPermisoListado(
                f.Solicitud.Id, f.Solicitud.SolicitanteId, f.NombreSolicitante, f.Solicitud.PermisoSolicitado,
                f.Solicitud.Estado.ToString(), f.Solicitud.FechaCreacion, f.Solicitud.AprobadorId,
                f.Solicitud.RechazadaPorId, f.Solicitud.MotivoRechazo, f.Solicitud.FechaResolucion))
            .ToList();
    }
}
