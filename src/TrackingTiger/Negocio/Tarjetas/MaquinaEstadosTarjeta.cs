namespace Tracking_Tiger.Negocio.Tarjetas;

// Quién ejecuta una transición de la tarjeta: el propio sistema (sin id) o un agente de fraude (con su id).
public enum EjecutorTarjeta
{
    Sistema,
    Agente
}

// RD-04: ÚNICO punto donde se resuelven las transiciones de estado de una Tarjeta.
// Agregar una transición = agregar una línea en Permitidas; ninguna otra clase valida estados.
// Estados declarados en EstadoTarjeta (RF-NEG-03). Tablas documentadas en docs/maquina-de-estados.md.
public static class MaquinaEstadosTarjeta
{
    // Una transición permitida: desde, hacia, quién la ejecuta, su condición y si exige una nota.
    public sealed record Transicion(EstadoTarjeta Desde, EstadoTarjeta Hacia, EjecutorTarjeta QuienLaEjecuta,
        string Condicion, bool ExigeNota);

    // Mismo orden que la tabla de transiciones permitidas de docs/maquina-de-estados.md.
    public static readonly IReadOnlyList<Transicion> Permitidas =
    [
        new(EstadoTarjeta.Activa, EstadoTarjeta.BloqueoSuave, EjecutorTarjeta.Sistema,
            "Solo el sistema, al detectar una ráfaga de declinaciones.", ExigeNota: false),
        new(EstadoTarjeta.BloqueoSuave, EstadoTarjeta.Activa, EjecutorTarjeta.Agente,
            "El cliente reconoció la actividad.", ExigeNota: false),
        new(EstadoTarjeta.BloqueoSuave, EstadoTarjeta.BloqueoTotal, EjecutorTarjeta.Agente,
            "El cliente no reconoce la actividad.", ExigeNota: true),
        new(EstadoTarjeta.BloqueoTotal, EstadoTarjeta.Activa, EjecutorTarjeta.Agente,
            "Permiso levantar-bloqueo-total aprobado y vigente.", ExigeNota: true),
        new(EstadoTarjeta.Activa, EstadoTarjeta.Cancelada, EjecutorTarjeta.Agente,
            "Pérdida o robo.", ExigeNota: true),
        new(EstadoTarjeta.BloqueoSuave, EstadoTarjeta.Cancelada, EjecutorTarjeta.Agente,
            "Pérdida o robo.", ExigeNota: true),
        new(EstadoTarjeta.BloqueoTotal, EstadoTarjeta.Cancelada, EjecutorTarjeta.Agente,
            "Pérdida o robo.", ExigeNota: true),
    ];

    // RF-NEG-04: transiciones prohibidas de forma explícita, con su motivo. Solo dan el mensaje del rechazo:
    // lo que decide es que no están en Permitidas. Cualquier otra combinación ausente también se rechaza.
    public static readonly IReadOnlyList<(EstadoTarjeta Desde, EstadoTarjeta Hacia, string Motivo)> ProhibidasExplicitas =
    [
        (EstadoTarjeta.Activa, EstadoTarjeta.BloqueoTotal, "Toda tarjeta pasa primero por Bloqueo Suave."),
        (EstadoTarjeta.BloqueoTotal, EstadoTarjeta.BloqueoSuave, "Un Bloqueo Total no se rebaja a Bloqueo Suave."),
        (EstadoTarjeta.Cancelada, EstadoTarjeta.Activa, "Una tarjeta cancelada por pérdida o robo nunca vuelve a Activa."),
    ];

    // RF-NEG-05: un estado es terminal si ninguna transición permitida parte de él (Cancelada).
    public static bool EsTerminal(EstadoTarjeta estado) => Permitidas.All(t => t.Desde != estado);

    public static Transicion? Buscar(EstadoTarjeta desde, EstadoTarjeta hacia) =>
        Permitidas.FirstOrDefault(t => t.Desde == desde && t.Hacia == hacia);

    // Intenta mover la tarjeta al estado indicado. Si no se permite, devuelve el rechazo con su motivo y la
    // tarjeta queda EXACTAMENTE igual (RF-NEG-04). Si se permite, devuelve la fila de historial a guardar.
    // - agenteId: id del agente que la ejecuta; null cuando la ejecuta el sistema.
    // - nota: obligatoria en las transiciones que la exigen.
    // - desdeEsperado: estado de origen que exige la operación. Por ejemplo, reconocer la actividad solo aplica
    //   a una tarjeta en BloqueoSuave: sin esto, sacaría a una tarjeta de BloqueoTotal sin el permiso.
    public static ResultadoTransicionTarjeta IntentarTransicion(Tarjeta tarjeta, EstadoTarjeta hacia, int? agenteId,
        DateTime ahoraUtc, string? nota = null, EstadoTarjeta? desdeEsperado = null)
    {
        if (desdeEsperado is not null && tarjeta.Estado != desdeEsperado)
            return ResultadoTransicionTarjeta.Rechazada(
                $"Esta operación solo aplica a una tarjeta en {desdeEsperado}; la tarjeta está en {tarjeta.Estado}.");

        var transicion = Buscar(tarjeta.Estado, hacia);
        if (transicion is null)
            return ResultadoTransicionTarjeta.Rechazada(MotivoNoPermitida(tarjeta.Estado, hacia));


        if (transicion.QuienLaEjecuta == EjecutorTarjeta.Agente && agenteId is null)
            return ResultadoTransicionTarjeta.Rechazada($"El paso a {hacia} exige un agente.");

        if (transicion.ExigeNota && string.IsNullOrWhiteSpace(nota))
            return ResultadoTransicionTarjeta.Rechazada($"Para pasar a {hacia} se exige una nota.");

        var anterior = tarjeta.Estado;
        tarjeta.AplicarEstado(hacia);
        var historial = HistorialEstadoTarjeta.Registrar(tarjeta.Id, anterior, hacia, agenteId, nota?.Trim(), ahoraUtc);
        return ResultadoTransicionTarjeta.Aplicada(hacia, historial);
    }

    private static string MotivoNoPermitida(EstadoTarjeta desde, EstadoTarjeta hacia)
    {
        var prohibida = ProhibidasExplicitas.FirstOrDefault(p => p.Desde == desde && p.Hacia == hacia);
        if (prohibida.Motivo is not null)
            return prohibida.Motivo;

        return EsTerminal(desde)
            ? $"La tarjeta está en {desde}, un estado terminal: ya no puede cambiar."
            : $"No se permite pasar de {desde} a {hacia}.";
    }
}

// Resultado de intentar una transición. Rechazada nunca cambia el estado ni trae historial.
public sealed record ResultadoTransicionTarjeta(bool Exito, string Mensaje, HistorialEstadoTarjeta? Historial)
{
    public static ResultadoTransicionTarjeta Aplicada(EstadoTarjeta nuevo, HistorialEstadoTarjeta historial) =>
        new(true, $"La tarjeta pasó a {nuevo}.", historial);

    public static ResultadoTransicionTarjeta Rechazada(string motivo) => new(false, motivo, null);
}
