namespace Tracking_Tiger.Core.Documentos;

// Pieza 3 del Core: registro de un documento subido (RF-DOC-01). El contenido del archivo no vive aquí,
// sino en el almacén (SeaweedFS), en RutaAlmacenamiento. Todas las fechas están en UTC (RD-11).
public class Documento
{
    public int Id { get; private set; }

    // Nombre original del archivo, tal como lo subió el usuario (solo para mostrar y descargar).
    public string Nombre { get; private set; } = string.Empty;

    // Tipo de contenido (por ejemplo, application/pdf).
    public string Tipo { get; private set; } = string.Empty;

    // Tamaño en bytes.
    public long Tamano { get; private set; }
    public DateTime FechaSubida { get; private set; }

    // Quién lo subió: el dueño del documento.
    public int UsuarioId { get; private set; }

    // Ruta dentro del almacén. Se genera en el servidor; nunca sale del nombre que manda el usuario.
    public string RutaAlmacenamiento { get; private set; } = string.Empty;

    // RF-DOC-04: la eliminación es lógica. El registro y el archivo siguen existiendo.
    public bool Eliminado { get; private set; }
    public DateTime? FechaEliminacion { get; private set; }

    // Solo para EF Core al leer filas. Todo documento nuevo se crea con Registrar.
    private Documento()
    {
    }

    // El servicio valida los datos antes de llamar aquí (RD-07).
    public static Documento Registrar(string nombre, string tipo, long tamano, int usuarioId, string rutaAlmacenamiento,
        DateTime ahoraUtc) =>
        new()
        {
            Nombre = nombre,
            Tipo = tipo,
            Tamano = tamano,
            UsuarioId = usuarioId,
            RutaAlmacenamiento = rutaAlmacenamiento,
            FechaSubida = ahoraUtc,
            Eliminado = false
        };

    internal void MarcarEliminado(DateTime ahoraUtc)
    {
        Eliminado = true;
        FechaEliminacion = ahoraUtc;
    }
}
