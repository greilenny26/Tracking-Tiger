namespace Tracking_Tiger.Core.Permisos;

// RF-GP-05: el permiso que recibe un usuario cuando se aprueba su solicitud. Le deja ejecutar UNA operación
// del catálogo hasta FechaVencimiento; después queda revocado solo, sin que nadie intervenga.
// La vigencia NO es un estado de la solicitud: la solicitud sigue en Aplicada (su tabla de transiciones
// no cambia, RF-GP-06). Todas las fechas están en UTC (RD-11).
public class PermisoOtorgado
{
    // Límites de la vigencia que elige el Administrador al aprobar: de 1 minuto a 30 días.
    public const int MinutosVigenciaMinimo = 1;
    public const int MinutosVigenciaMaximo = 30 * 24 * 60;

    public int Id { get; private set; }
    public int UsuarioId { get; private set; }

    // Nombre de la operación del catálogo, por ejemplo "listar-usuarios".
    public string Operacion { get; private set; } = string.Empty;

    // Solicitud que originó este permiso (para el historial).
    public int SolicitudId { get; private set; }

    public DateTime FechaOtorgamiento { get; private set; }
    public DateTime FechaVencimiento { get; private set; }

    // Solo para EF Core al leer filas. Todo permiso nuevo se crea con Otorgar.
    private PermisoOtorgado()
    {
    }

    // El servicio valida minutosVigencia antes de llamar aquí (RD-07).
    public static PermisoOtorgado Otorgar(SolicitudPermiso solicitud, int minutosVigencia, DateTime ahoraUtc) =>
        new()
        {
            UsuarioId = solicitud.SolicitanteId,
            Operacion = solicitud.PermisoSolicitado,
            SolicitudId = solicitud.Id,
            FechaOtorgamiento = ahoraUtc,
            FechaVencimiento = ahoraUtc.AddMinutes(minutosVigencia)
        };

    public bool EstaVigente(DateTime ahoraUtc) => ahoraUtc < FechaVencimiento;
}
