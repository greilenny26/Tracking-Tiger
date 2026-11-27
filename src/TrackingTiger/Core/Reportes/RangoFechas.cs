using System.Globalization;

namespace Tracking_Tiger.Core.Reportes;

// Rango de fechas de un reporte (RF-REP-05): días completos en UTC, ambos incluidos y opcionales.
// Desde es el primer instante incluido; Antes es el primer instante que ya queda fuera.
public sealed record RangoFechas(DateTime? Desde, DateTime? Antes)
{
    private const string Formato = "yyyy-MM-dd";

    // Devuelve el rango, o un mensaje si una fecha no es válida (RD-07).
    public static (RangoFechas? Rango, string? Error) Leer(string? desde, string? hasta)
    {
        if (!TryLeerDia(desde, out var inicio))
            return (null, $"desde debe ser una fecha con el formato {Formato}.");
        if (!TryLeerDia(hasta, out var fin))
            return (null, $"hasta debe ser una fecha con el formato {Formato}.");
        if (inicio > fin)
            return (null, "desde no puede ser posterior a hasta.");

        // El último día posible no tiene día siguiente: no hace falta límite superior.
        DateTime? antes = fin is null || fin.Value.Date == DateTime.MaxValue.Date ? null : fin.Value.AddDays(1);

        return (new RangoFechas(inicio, antes), null);
    }

    private static bool TryLeerDia(string? texto, out DateTime? dia)
    {
        dia = null;
        if (string.IsNullOrWhiteSpace(texto))
            return true;

        if (!DateTime.TryParseExact(texto.Trim(), Formato, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            return false;

        dia = DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
        return true;
    }
}
