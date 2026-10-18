namespace Tracking_Tiger.Core.Permisos;

// Fila de una solicitud de permiso en las respuestas de la API (RF-GP-02). Todas las fechas en UTC.
public sealed record SolicitudPermisoListado(
    int Id,
    int SolicitanteId,
    string Solicitante,
    string PermisoSolicitado,
    string Estado,
    DateTime FechaCreacion,
    int? AprobadorId,
    int? RechazadaPorId,
    string? MotivoRechazo,
    DateTime? FechaResolucion);
