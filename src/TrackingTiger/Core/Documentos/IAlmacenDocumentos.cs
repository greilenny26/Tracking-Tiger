namespace Tracking_Tiger.Core.Documentos;

// Dónde se guarda el contenido de los documentos (RF-DOC-05). El servicio no sabe si es SeaweedFS u otra cosa.
// Si el almacén falla, la implementación lanza ErrorAlmacenDocumentos con un mensaje seguro para el usuario.
public interface IAlmacenDocumentos
{
    Task GuardarAsync(string ruta, Stream contenido, string tipo, CancellationToken cancelacion = default);

    // Devuelve null si el archivo no existe en el almacén.
    Task<Stream?> AbrirAsync(string ruta, CancellationToken cancelacion = default);
}

// Falla del almacén (no configurado, caído o que respondió con error). El mensaje nunca expone detalles internos (RD-08).
public sealed class ErrorAlmacenDocumentos(string mensaje) : Exception(mensaje);
