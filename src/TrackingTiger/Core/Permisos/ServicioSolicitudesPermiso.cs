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

        // Si ya tiene ese permiso vigente no hace falta pedirlo; podrá pedirlo de nuevo cuando venza.
        var ahora = _reloj.AhoraUtc;
        var vigente = await _contexto.PermisosOtorgados
            .Where(p => p.UsuarioId == solicitanteId && p.Operacion == permiso && p.FechaVencimiento > ahora)
            .OrderByDescending(p => p.FechaVencimiento)
            .FirstOrDefaultAsync();
        if (vigente is not null)
            return ResultadoCreacionSolicitud.YaVigente(vigente.FechaVencimiento);

        var solicitud = SolicitudPermiso.Crear(solicitanteId, permiso, _reloj.AhoraUtc);
        _contexto.SolicitudesPermiso.Add(solicitud);
        await _contexto.SaveChangesAsync();

        var listado = (await ListarAsync(s => s.Id == solicitud.Id)).Single();
        return ResultadoCreacionSolicitud.Creada(listado);
    }

    // RF-GP-03 y RF-GP-05: el Administrador aprueba y elige cuántos minutos durará el permiso. Primero la máquina
    // valida Pendiente → Aprobada (registra quién y cuándo, RF-GP-07); luego se otorga el permiso y el sistema
    // pasa la solicitud a Aplicada. Todo se guarda en un solo SaveChanges: o queda todo o no queda nada.
    public async Task<ResultadoResolucionSolicitud> AprobarAsync(int solicitudId, int administradorId, int? minutosVigencia)
    {
        if (minutosVigencia is null
            || minutosVigencia < PermisoOtorgado.MinutosVigenciaMinimo
            || minutosVigencia > PermisoOtorgado.MinutosVigenciaMaximo)
            return ResultadoResolucionSolicitud.DatoInvalido(
                $"minutosVigencia es obligatorio y debe estar entre {PermisoOtorgado.MinutosVigenciaMinimo} " +
                $"y {PermisoOtorgado.MinutosVigenciaMaximo} (30 días).");

        var solicitud = await _contexto.SolicitudesPermiso.SingleOrDefaultAsync(s => s.Id == solicitudId);
        if (solicitud is null)
            return ResultadoResolucionSolicitud.NoEncontrada();

        if (!await EsAdministradorAsync(administradorId))
            return ResultadoResolucionSolicitud.NoPermitida("Solo un Administrador puede aprobar solicitudes.");

        var ahora = _reloj.AhoraUtc;
        var aprobacion = MaquinaEstadosSolicitud.IntentarTransicion(solicitud, EstadoSolicitud.Aprobada, administradorId, ahora);
        if (!aprobacion.Exito)
            return ResultadoResolucionSolicitud.NoPermitida(aprobacion.Mensaje);

        // RF-GP-05: el cambio de permiso se aplica solo, sin intervención manual.
        _contexto.PermisosOtorgados.Add(PermisoOtorgado.Otorgar(solicitud, minutosVigencia.Value, ahora));
        var aplicacion = MaquinaEstadosSolicitud.IntentarTransicion(solicitud, EstadoSolicitud.Aplicada, actorId: null, ahora);
        if (!aplicacion.Exito)
            return ResultadoResolucionSolicitud.NoPermitida(aplicacion.Mensaje);

        await _contexto.SaveChangesAsync();
        return ResultadoResolucionSolicitud.Resuelta((await ListarAsync(s => s.Id == solicitudId)).Single());
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

        if (!await EsAdministradorAsync(administradorId))
            return ResultadoResolucionSolicitud.NoPermitida("Solo un Administrador puede rechazar solicitudes.");

        var transicion =MaquinaEstadosSolicitud.IntentarTransicion(
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

    // RF-GP-03 y RF-GP-04: solo un Administrador resuelve solicitudes. CatalogoOperaciones ya lo exige en el
    // endpoint; el servicio lo vuelve a comprobar para que la regla viva también en Gestión de permisos.
    private Task<bool> EsAdministradorAsync(int usuarioId) =>
        _contexto.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Rol == Rol.Administrador);

    private async Task<IReadOnlyList<SolicitudPermisoListado>> ListarAsync(Expression<Func<SolicitudPermiso, bool>> filtro)
    {
        var filas = await _contexto.SolicitudesPermiso
            .AsNoTracking()
            .Where(filtro)
            .Join(_contexto.Usuarios, s => s.SolicitanteId, u => u.Id, (s, u) => new { Solicitud = s, NombreSolicitante = u.Nombre })
            .OrderBy(f => f.Solicitud.FechaCreacion)
            .ThenBy(f => f.Solicitud.Id)
            .ToListAsync();

        // Fecha de vencimiento del permiso que otorgó cada solicitud aprobada (RF-GP-05).
        var ids = filas.Select(f => f.Solicitud.Id).ToList();
        var vencimientos = await _contexto.PermisosOtorgados
            .AsNoTracking()
            .Where(p => ids.Contains(p.SolicitudId))
            .ToDictionaryAsync(p => p.SolicitudId, p => p.FechaVencimiento);

        return filas
            .Select(f => new SolicitudPermisoListado(
                f.Solicitud.Id, f.Solicitud.SolicitanteId, f.NombreSolicitante, f.Solicitud.PermisoSolicitado,
                f.Solicitud.Estado.ToString(), f.Solicitud.FechaCreacion, f.Solicitud.AprobadorId,
                f.Solicitud.RechazadaPorId, f.Solicitud.MotivoRechazo, f.Solicitud.FechaResolucion,
                vencimientos.TryGetValue(f.Solicitud.Id, out var vence) ? vence : null))
            .ToList();
    }
}
