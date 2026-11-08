namespace Tracking_Tiger.Core.Documentos;

public enum EstadoOperacionDocumento
{
    Exito,
    // No existe, o está eliminado (RF-DOC-04: un documento eliminado ya no se muestra).
    NoEncontrado,
    // No es el dueño ni un Administrador (RF-DOC-03).
    SinPermiso,
    DatoInvalido,
    // El almacén de archivos no está configurado o no responde.
    AlmacenNoDisponible
}

// Resultado de una operación del Manejador de documentos. Descarga solo viene al descargar.
public sealed record ResultadoOperacionDocumento(EstadoOperacionDocumento Estado, string Mensaje,
    DocumentoDetalle? Documento = null, DocumentoDescarga? Descarga = null)
{
    public static ResultadoOperacionDocumento Exito(string mensaje, DocumentoDetalle documento) =>
        new(EstadoOperacionDocumento.Exito, mensaje, documento);

    public static ResultadoOperacionDocumento ExitoDescarga(DocumentoDescarga descarga) =>
        new(EstadoOperacionDocumento.Exito, "Documento listo para descargar.", Descarga: descarga);

    public static ResultadoOperacionDocumento NoEncontrado() =>
        new(EstadoOperacionDocumento.NoEncontrado, "No existe un documento con ese id.");

    public static ResultadoOperacionDocumento SinPermiso() =>
        new(EstadoOperacionDocumento.SinPermiso, "Solo el dueño del documento o un Administrador pueden hacer esta operación.");

    public static ResultadoOperacionDocumento DatoInvalido(string mensaje) => new(EstadoOperacionDocumento.DatoInvalido, mensaje);

    public static ResultadoOperacionDocumento AlmacenNoDisponible(string mensaje) =>
        new(EstadoOperacionDocumento.AlmacenNoDisponible, mensaje);
}

// Datos de un documento en las respuestas de la API (RF-DOC-01). Nunca la ruta interna del almacén. Fechas en UTC.
public sealed record DocumentoDetalle(int Id, string Nombre, string Tipo, long Tamano, DateTime FechaSubida,
    int UsuarioId, string SubidoPor);

// Contenido para descargar. Quien lo recibe debe cerrar Contenido.
public sealed record DocumentoDescarga(Stream Contenido, string Nombre, string Tipo);
