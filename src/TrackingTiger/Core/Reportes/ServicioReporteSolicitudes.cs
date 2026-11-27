using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.ControlAcceso;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.Reportes;

// Reporte del Core (RF-REP-01, 03): solicitudes de permiso por día de creación y estado.
// La agrupación y el conteo los hace la base de datos.
public sealed class ServicioReporteSolicitudes
{
    private readonly ContextoDatos _contexto;

    public ServicioReporteSolicitudes(ContextoDatos contexto)
    {
        _contexto = contexto;
    }

    // RF-REP-04: el Administrador resume las de todos; cualquier otro usuario, solo las suyas.
    // El id llega de la sesión y el rol se lee de la base, nunca de la petición.
    public async Task<IReadOnlyList<FilaReporteSolicitudes>> GenerarAsync(int usuarioId, RangoFechas rango)
    {
        var esAdministrador = await _contexto.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Rol == Rol.Administrador);

        var consulta = _contexto.SolicitudesPermiso.AsNoTracking();
        if (!esAdministrador)
            consulta = consulta.Where(s => s.SolicitanteId == usuarioId);
        if (rango.Desde is not null)
            consulta = consulta.Where(s => s.FechaCreacion >= rango.Desde.Value);
        if (rango.Antes is not null)
            consulta = consulta.Where(s => s.FechaCreacion < rango.Antes.Value);

        var grupos = await consulta
            .GroupBy(s => new { Dia = s.FechaCreacion.Date, s.Estado })
            .Select(g => new { g.Key.Dia, g.Key.Estado, Total = g.Count() })
            .ToListAsync();

        return grupos
            .OrderBy(g => g.Dia)
            .ThenBy(g => g.Estado)
            .Select(g => new FilaReporteSolicitudes(g.Dia.ToString("yyyy-MM-dd"), g.Estado.ToString(), g.Total))
            .ToList();
    }
}

public sealed record FilaReporteSolicitudes(string Periodo, string Estado, int Total);
