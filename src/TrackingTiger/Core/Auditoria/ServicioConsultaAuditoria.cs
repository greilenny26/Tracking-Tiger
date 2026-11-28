using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Persistencia;
using Tracking_Tiger.Core.Reportes;

namespace Tracking_Tiger.Core.Auditoria;

// RF-AUD-07: consulta de la auditoría filtrable por usuario, entidad y rango de fechas.
// Solo lee: la pieza no tiene ninguna operación para editar ni borrar registros (RF-AUD-06).
// Que sea del Administrador lo declara CatalogoOperaciones.
public sealed class ServicioConsultaAuditoria
{
    private readonly ContextoDatos _contexto;

    public ServicioConsultaAuditoria(ContextoDatos contexto)
    {
        _contexto = contexto;
    }

    // Valida los filtros tal como llegan de la petición (RD-07) y consulta. Error es null si todo salió bien.
    public async Task<(IReadOnlyList<RegistroAuditoriaListado>? Registros, string? Error)> ConsultarAsync(
        string? usuarioId, string? entidad, string? desde, string? hasta)
    {
        int? idUsuario = null;
        if (!string.IsNullOrWhiteSpace(usuarioId))
        {
            if (!int.TryParse(usuarioId.Trim(), out var id) || id <= 0)
                return (null, "usuarioId debe ser un número entero positivo.");
            idUsuario = id;
        }

        string? nombreEntidad = null;
        if (!string.IsNullOrWhiteSpace(entidad))
        {
            nombreEntidad = EntidadesAuditadas.Todas
                .FirstOrDefault(e => e.Equals(entidad.Trim(), StringComparison.OrdinalIgnoreCase));
            if (nombreEntidad is null)
                return (null, $"entidad debe ser una de estas: {string.Join(", ", EntidadesAuditadas.Todas)}.");
        }

        var (rango, errorRango) = RangoFechas.Leer(desde, hasta);
        if (rango is null)
            return (null, errorRango);

        return (await ListarAsync(idUsuario, nombreEntidad, rango), null);
    }

    public async Task<IReadOnlyList<RegistroAuditoriaListado>> ListarAsync(int? usuarioId, string? entidad, RangoFechas rango)
    {
        var consulta = _contexto.RegistrosAuditoria.AsNoTracking();
        if (usuarioId is not null)
            consulta = consulta.Where(r => r.UsuarioId == usuarioId);
        if (entidad is not null)
            consulta = consulta.Where(r => r.Entidad == entidad);
        if (rango.Desde is not null)
            consulta = consulta.Where(r => r.Fecha >= rango.Desde.Value);
        if (rango.Antes is not null)
            consulta = consulta.Where(r => r.Fecha < rango.Antes.Value);

        return await consulta
            .OrderByDescending(r => r.Fecha)
            .ThenByDescending(r => r.Id)
            .Join(_contexto.Usuarios, r => r.UsuarioId, u => u.Id, (r, u) => new RegistroAuditoriaListado(
                r.Id, r.UsuarioId, u.Nombre, r.Accion, r.Entidad, r.EntidadId, r.Fecha, r.ValorAnterior, r.ValorNuevo))
            .ToListAsync();
    }
}

public sealed record RegistroAuditoriaListado(long Id, int UsuarioId, string NombreUsuario, string Accion,
    string Entidad, int EntidadId, DateTime Fecha, string ValorAnterior, string ValorNuevo);
