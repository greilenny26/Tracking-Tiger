namespace Tracking_Tiger.Core.Auditoria;

// Pieza 6 del Core: constancia de quién hizo qué, sobre qué y cuándo (RF-AUD-01).
// Solo se crea con Crear y no tiene ningún método que la cambie (RF-AUD-06). Fechas en UTC (RD-11).
public class RegistroAuditoria
{
    public const int LargoMaximoAccion = 50;
    public const int LargoMaximoEntidad = 50;
    public const int LargoMaximoValor = 600;

    public long Id { get; private set; }

    // Usuario que actuó.
    public int UsuarioId { get; private set; }

    public string Accion { get; private set; } = string.Empty;

    // Entidad afectada: uno de los nombres de EntidadesAuditadas.
    public string Entidad { get; private set; } = string.Empty;

    public int EntidadId { get; private set; }

    public DateTime Fecha { get; private set; }

    public string ValorAnterior { get; private set; } = string.Empty;

    public string ValorNuevo { get; private set; } = string.Empty;

    // Solo para EF Core al leer filas.
    private RegistroAuditoria()
    {
    }

    // RF-AUD-01: un registro al que le falte un dato no se crea.
    public static RegistroAuditoria Crear(int usuarioId, string accion, string entidad, int entidadId,
        string valorAnterior, string valorNuevo, DateTime ahoraUtc)
    {
        if (string.IsNullOrWhiteSpace(accion) || string.IsNullOrWhiteSpace(entidad)
            || string.IsNullOrWhiteSpace(valorAnterior) || string.IsNullOrWhiteSpace(valorNuevo))
            throw new ArgumentException("Un registro de auditoría necesita todos sus datos.");

        return new RegistroAuditoria
        {
            UsuarioId = usuarioId,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            Fecha = ahoraUtc,
            ValorAnterior = Recortar(valorAnterior),
            ValorNuevo = Recortar(valorNuevo)
        };
    }

    private static string Recortar(string valor) =>
        valor.Length <= LargoMaximoValor ? valor : valor[..LargoMaximoValor];
}

// Nombres de las entidades auditadas: el único lugar donde se declaran (también los usa el filtro de la consulta).
public static class EntidadesAuditadas
{
    public const string SolicitudPermiso = "SolicitudPermiso";
    public const string Documento = "Documento";
    public const string Usuario = "Usuario";

    public static readonly IReadOnlyList<string> Todas = [SolicitudPermiso, Documento, Usuario];
}

// Nombres de las acciones auditadas.
public static class AccionesAuditadas
{
    public const string AprobarSolicitud = "AprobarSolicitud";
    public const string RechazarSolicitud = "RechazarSolicitud";
    public const string SubirDocumento = "SubirDocumento";
    public const string EliminarDocumento = "EliminarDocumento";
    public const string CambiarRol = "CambiarRol";
    public const string RestablecerContrasena = "RestablecerContrasena";
    public const string ForzarRestablecimiento = "ForzarRestablecimiento";
    public const string CambiarContrasenaPropia = "CambiarContrasenaPropia";

    // RF-AUD-05: se guarda quién lo inició, nunca la contraseña ni el código.
    public const string IniciadoPorUsuario = "Iniciado por: Usuario";
    public const string IniciadoPorAdministrador = "Iniciado por: Administrador";
}
