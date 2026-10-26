namespace Tracking_Tiger.Core.Permisos;

// Quién ejecuta una transición: un Administrador (con su id) o el propio sistema (sin id).
public enum Ejecutor
{
    Administrador,
    Sistema
}

// RD-04: ÚNICO punto donde se resuelven las transiciones de una SolicitudPermiso.
// Agregar una transición = agregar una línea en Permitidas; ninguna otra clase valida estados.
// Cualquier combinación que no esté en Permitidas está prohibida (RF-GP-06).
public static class MaquinaEstadosSolicitud
{
    // Una transición permitida: desde, hacia, quién la ejecuta, su condición y si exige motivo.
    public sealed record Transicion(EstadoSolicitud Desde, EstadoSolicitud Hacia, Ejecutor QuienLaEjecuta,
        string Condicion, bool ExigeMotivo);

    public static readonly IReadOnlyList<Transicion> Permitidas =
    [
        new(EstadoSolicitud.Pendiente, EstadoSolicitud.Aprobada, Ejecutor.Administrador,
            "La solicitud está en Pendiente.", ExigeMotivo: false),
        new(EstadoSolicitud.Aprobada, EstadoSolicitud.Aplicada, Ejecutor.Sistema,
            "Automática, inmediatamente después de aprobar.", ExigeMotivo: false),
        new(EstadoSolicitud.Pendiente, EstadoSolicitud.Rechazada, Ejecutor.Administrador,
            "Exige un motivo escrito.", ExigeMotivo: true),
    ];

    // Un estado es terminal si ninguna transición permitida parte de él (Aplicada, Rechazada).
    public static bool EsTerminal(EstadoSolicitud estado) => Permitidas.All(t => t.Desde != estado);

    public static Transicion? Buscar(EstadoSolicitud desde, EstadoSolicitud hacia) =>
        Permitidas.FirstOrDefault(t => t.Desde == desde && t.Hacia == hacia);

    // Intenta mover la solicitud al estado indicado. Si no se permite, devuelve el rechazo con su motivo
    // y la solicitud queda EXACTAMENTE igual (RF-GP-06).
    // - actorId: id del Administrador que ejecuta la transición; null cuando la ejecuta el sistema.
    // - motivo: obligatorio para rechazar (RF-GP-04).
    public static ResultadoTransicionSolicitud IntentarTransicion(SolicitudPermiso solicitud, EstadoSolicitud hacia,
        int? actorId, DateTime ahoraUtc, string? motivo = null)
    {
        if (EsTerminal(solicitud.Estado))
            return ResultadoTransicionSolicitud.Rechazada(
                $"La solicitud ya está en {solicitud.Estado}: no puede cambiar de estado.");

        var transicion = Buscar(solicitud.Estado, hacia);
        if (transicion is null)
            return ResultadoTransicionSolicitud.Rechazada($"No se permite pasar de {solicitud.Estado} a {hacia}.");

        if (transicion.QuienLaEjecuta == Ejecutor.Sistema && actorId is not null)
            return ResultadoTransicionSolicitud.Rechazada($"El paso a {hacia} lo hace el sistema, no un usuario.");

        if (transicion.QuienLaEjecuta == Ejecutor.Administrador)
        {
            if (actorId is null)
                return ResultadoTransicionSolicitud.Rechazada($"El paso a {hacia} exige un Administrador.");

            // RF-GP-07: nadie resuelve su propia solicitud.
            if (actorId == solicitud.SolicitanteId)
                return ResultadoTransicionSolicitud.Rechazada("No puedes aprobar ni rechazar tu propia solicitud.");
        }

        if (transicion.ExigeMotivo && motivo is null)
            return ResultadoTransicionSolicitud.Rechazada($"Para pasar a {hacia} se exige un motivo escrito.");

        switch (hacia)
        {
            case EstadoSolicitud.Aprobada:
                solicitud.RegistrarAprobacion(actorId!.Value, ahoraUtc);
                break;
            case EstadoSolicitud.Rechazada:
                solicitud.RegistrarRechazo(actorId!.Value, motivo!.Trim(), ahoraUtc);
                break;
            case EstadoSolicitud.Aplicada:
                solicitud.RegistrarAplicacion();
                break;
        }

        return ResultadoTransicionSolicitud.Aplicada(hacia);
    }
}

// Resultado de intentar una transición. Rechazada nunca cambia el estado.
public sealed record ResultadoTransicionSolicitud(bool Exito, string Mensaje)
{
    public static ResultadoTransicionSolicitud Aplicada(EstadoSolicitud nuevo) =>
        new(true, $"La solicitud pasó a {nuevo}.");

    public static ResultadoTransicionSolicitud Rechazada(string motivo) => new(false, motivo);
}
