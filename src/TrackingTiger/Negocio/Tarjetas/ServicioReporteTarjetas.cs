using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Persistencia;
using Tracking_Tiger.Core.Reportes;

namespace Tracking_Tiger.Negocio.Tarjetas;

// Reporte del negocio (RF-REP-02, 03): ¿en qué comercios se concentran las compras declinadas y cuánto
// dinero representan? Sirve para decidir qué comercios revisar por posible fraude.
// La base de datos agrupa, cuenta y suma; los montos están en DOP.
public sealed class ServicioReporteTarjetas
{
    private readonly ContextoDatos _contexto;

    public ServicioReporteTarjetas(ContextoDatos contexto)
    {
        _contexto = contexto;
    }

    public async Task<IReadOnlyList<FilaRiesgoComercio>> RiesgoPorComercioAsync(RangoFechas rango)
    {
        var consulta = _contexto.Set<Transaccion>().AsNoTracking();
        if (rango.Desde is not null)
            consulta = consulta.Where(t => t.Fecha >= rango.Desde.Value);
        if (rango.Antes is not null)
            consulta = consulta.Where(t => t.Fecha < rango.Antes.Value);

        var grupos = await consulta
            .GroupBy(t => t.Comercio)
            .Select(g => new
            {
                Comercio = g.Key,
                Transacciones = g.Count(),
                Declinadas = g.Count(t => !t.Aprobada),
                MontoDeclinado = g.Sum(t => t.Aprobada ? 0m : t.Monto),
                MontoAprobado = g.Sum(t => t.Aprobada ? t.Monto : 0m)
            })
            .ToListAsync();

        return grupos
            .OrderByDescending(g => g.MontoDeclinado)
            .ThenBy(g => g.Comercio)
            .Select(g => new FilaRiesgoComercio(g.Comercio, g.Transacciones, g.Declinadas,
                Math.Round(100m * g.Declinadas / g.Transacciones, 2, MidpointRounding.AwayFromZero),
                g.MontoDeclinado, g.MontoAprobado))
            .ToList();
    }
}

public sealed record FilaRiesgoComercio(string Comercio, int Transacciones, int Declinadas,
    decimal PorcentajeDeclinacion, decimal MontoDeclinado, decimal MontoAprobado);
