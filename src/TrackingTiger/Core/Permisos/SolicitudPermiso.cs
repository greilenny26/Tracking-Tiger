namespace Tracking_Tiger.Core.Permisos;

// Solicitud de permiso (pieza 2, Gestión de permisos). Un usuario pide poder ejecutar una operación
// del catálogo (PermisoSolicitado). Su estado solo cambia a través de MaquinaEstadosSolicitud (RD-04).
// Todas las fechas están en UTC (RD-11).
public class SolicitudPermiso
{
    public int Id { get; private set; }

    public int SolicitanteId { get; private set; }

    // Nombre de la operación del catálogo que se pide, por ejemplo "listar-usuarios".
    public string PermisoSolicitado { get; private set; } = string.Empty;

    public EstadoSolicitud Estado { get; private set; }

    // RF-GP-03: quién aprobó. La fecha queda en FechaResolucion.
    public int? AprobadorId { get; private set; }

    // RF-GP-04: quién rechazó y por qué.
    public int? RechazadaPorId { get; private set; }
    public string? MotivoRechazo { get; private set; }

    public DateTime FechaCreacion { get; private set; }
    public DateTime? FechaResolucion { get; private set; }

    // Solo para EF Core al leer filas. Toda solicitud nueva se crea con Crear.
    private SolicitudPermiso()
    {
    }

    // RF-GP-01: toda solicitud nace en Pendiente.
    public static SolicitudPermiso Crear(int solicitanteId, string permisoSolicitado, DateTime ahoraUtc) =>
        new()
        {
            SolicitanteId = solicitanteId,
            PermisoSolicitado = permisoSolicitado,
            Estado = EstadoSolicitud.Pendiente,
            FechaCreacion = ahoraUtc
        };

    // Los tres métodos siguientes solo los invoca MaquinaEstadosSolicitud después de validar la
    // transición (RD-04): la entidad no decide qué transiciones existen.
    internal void RegistrarAprobacion(int aprobadorId, DateTime ahoraUtc)
    {
        Estado = EstadoSolicitud.Aprobada;
        AprobadorId = aprobadorId;
        FechaResolucion = ahoraUtc;
    }

    internal void RegistrarRechazo(int rechazadaPorId, string motivo, DateTime ahoraUtc)
    {
        Estado = EstadoSolicitud.Rechazada;
        RechazadaPorId = rechazadaPorId;
        MotivoRechazo = motivo;
        FechaResolucion = ahoraUtc;
    }

    internal void RegistrarAplicacion() => Estado = EstadoSolicitud.Aplicada;
}
