using Tracking_Tiger.Core.ControlAcceso;

namespace Tracking_Tiger.Negocio.Alertas;

// RD-04: ÚNICO punto donde se resuelven las transiciones de estado de una AlertaFraude.
// Agregar una transición nueva = agregar una línea en Permitidas; ninguna otra clase valida estados.
// Estados declarados en EstadoAlerta (RF-NEG-03). Tabla documentada en docs/maquina-de-estados.md.
public static class MaquinaEstadosAlerta
{
    // Una transición permitida: desde, hacia, quién la ejecuta y su condición.
    public sealed record Transicion(EstadoAlerta Desde, EstadoAlerta Hacia, Rol[] QuienLaEjecuta, string Condicion,
        bool ExigeObservacion);

    public static readonly IReadOnlyList<Transicion> Permitidas =
    [
        new(EstadoAlerta.Detectada, EstadoAlerta.EnRevision, [Rol.Estandar, Rol.Administrador],
            "La alerta está en Detectada.", ExigeObservacion: false),
        new(EstadoAlerta.EnRevision, EstadoAlerta.Confirmada, [Rol.Administrador],
            "La alerta está en EnRevision y se indica una observación.", ExigeObservacion: true),
        new(EstadoAlerta.EnRevision, EstadoAlerta.Descartada, [Rol.Administrador],
            "La alerta está en EnRevision y se indica una observación.", ExigeObservacion: true),
    ];

    // RF-NEG-04: transiciones prohibidas de forma explícita, con su motivo. Cualquier otra combinación que no
    // esté en Permitidas también se rechaza.
    public static readonly IReadOnlyList<(EstadoAlerta Desde, EstadoAlerta Hacia, string Motivo)> ProhibidasExplicitas =
    [
        (EstadoAlerta.Detectada, EstadoAlerta.Confirmada, "Una alerta debe revisarse antes de confirmarse."),
        (EstadoAlerta.Descartada, EstadoAlerta.Confirmada, "Una alerta descartada no puede confirmarse después."),
    ];

    // RF-NEG-05: un estado es terminal si ninguna transición permitida parte de él (Confirmada, Descartada).
    public static bool EsTerminal(EstadoAlerta estado) => Permitidas.All(t => t.Desde != estado);

    public static Transicion? Buscar(EstadoAlerta desde, EstadoAlerta hacia) =>
        Permitidas.FirstOrDefault(t => t.Desde == desde && t.Hacia == hacia);

    // Intenta mover la alerta al estado indicado. Si la transición no está permitida (o falta la observación
    // que exige), se rechaza con un mensaje en español y el estado NO cambia.
    public static ResultadoTransicion IntentarTransicion(AlertaFraude alerta, EstadoAlerta hacia, DateTime ahoraUtc,
        string? observacion = null)
    {
        var prohibida = ProhibidasExplicitas.FirstOrDefault(p => p.Desde == alerta.Estado && p.Hacia == hacia);
        if (prohibida.Motivo is not null)
            return ResultadoTransicion.Rechazada(prohibida.Motivo);

        if (EsTerminal(alerta.Estado))
            return ResultadoTransicion.Rechazada($"La alerta está en {alerta.Estado}, un estado terminal: ya no puede cambiar.");

        var transicion = Buscar(alerta.Estado, hacia);
        if (transicion is null)
            return ResultadoTransicion.Rechazada($"No se permite pasar de {alerta.Estado} a {hacia}.");

        if (transicion.ExigeObservacion && string.IsNullOrWhiteSpace(observacion))
            return ResultadoTransicion.Rechazada($"Para pasar a {hacia} se exige una observación.");

        alerta.AplicarEstado(hacia, EsTerminal(hacia) ? ahoraUtc : null, observacion?.Trim());
        return ResultadoTransicion.Aplicada(hacia);
    }
}

// Resultado de intentar una transición. Rechazada nunca cambia el estado.
public sealed record ResultadoTransicion(bool Exito, string Mensaje)
{
    public static ResultadoTransicion Aplicada(EstadoAlerta nuevo) => new(true, $"La alerta pasó a {nuevo}.");

    public static ResultadoTransicion Rechazada(string motivo) => new(false, motivo);
}
