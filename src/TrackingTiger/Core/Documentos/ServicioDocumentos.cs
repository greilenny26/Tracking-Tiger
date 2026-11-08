using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.ControlAcceso;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.Documentos;

// Lógica del Manejador de documentos (RD-02). CatalogoOperaciones exige sesión en todas sus operaciones; el id del
// usuario llega siempre de la sesión del servidor. Quién puede ver, descargar o eliminar cada documento se decide
// aquí, en el servidor (RD-06): así una petición construida a mano también se rechaza.
public sealed class ServicioDocumentos
{
    public const long TamanoMaximoBytes = 10 * 1024 * 1024;
    public const int LargoMaximoNombre = 255;
    private const int LargoMaximoTipo = 100;
    private const string TipoPredeterminado = "application/octet-stream";
    private const string CarpetaAlmacen = "documentos";

    private readonly ContextoDatos _contexto;
    private readonly IReloj _reloj;
    private readonly IAlmacenDocumentos _almacen;

    public ServicioDocumentos(ContextoDatos contexto, IReloj reloj, IAlmacenDocumentos almacen)
    {
        _contexto = contexto;
        _reloj = reloj;
        _almacen = almacen;
    }

    // RF-DOC-01: guarda el archivo en el almacén y registra nombre, tipo, tamaño, fecha y quién lo subió.
    // RD-07: un archivo ausente o vacío se rechaza con un mensaje, sin tocar el almacén ni la base.
    public async Task<ResultadoOperacionDocumento> SubirAsync(int usuarioId, string? nombre, string? tipo, long tamano,
        Stream? contenido)
    {
        if (contenido is null)
            return ResultadoOperacionDocumento.DatoInvalido("Falta el archivo. Envíalo en el campo 'archivo' del formulario.");

        if (tamano <= 0)
            return ResultadoOperacionDocumento.DatoInvalido("El archivo está vacío.");

        if (tamano > TamanoMaximoBytes)
            return ResultadoOperacionDocumento.DatoInvalido($"El archivo supera el tamaño máximo de {TamanoMaximoBytes / (1024 * 1024)} MB.");

        // Solo el nombre, sin carpetas: "../../x.txt" queda como "x.txt".
        var nombreLimpio = Path.GetFileName(nombre?.Trim() ?? string.Empty);
        if (string.IsNullOrWhiteSpace(nombreLimpio) || nombreLimpio.Length > LargoMaximoNombre)
            return ResultadoOperacionDocumento.DatoInvalido($"El archivo debe tener un nombre de como máximo {LargoMaximoNombre} caracteres.");

        var tipoLimpio = string.IsNullOrWhiteSpace(tipo) || tipo.Trim().Length > LargoMaximoTipo ? TipoPredeterminado : tipo.Trim();

        var usuario = await _contexto.Usuarios.SingleOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario is null)
            return ResultadoOperacionDocumento.SinPermiso();

        // La ruta la genera el servidor: el nombre del usuario nunca decide dónde se escribe.
        var ruta = $"{CarpetaAlmacen}/{Guid.NewGuid():N}";
        try
        {
            await _almacen.GuardarAsync(ruta, contenido, tipoLimpio);
        }
        catch (ErrorAlmacenDocumentos error)
        {
            return ResultadoOperacionDocumento.AlmacenNoDisponible(error.Message);
        }

        var documento = Documento.Registrar(nombreLimpio, tipoLimpio, tamano, usuarioId, ruta, _reloj.AhoraUtc);
        _contexto.Documentos.Add(documento);
        await _contexto.SaveChangesAsync();
        return ResultadoOperacionDocumento.Exito("Documento subido.", Detalle(documento, usuario.Nombre));
    }

    // RF-DOC-02: nunca los eliminados. El Administrador ve los de todos; cualquier otro usuario, solo los suyos.
    public async Task<IReadOnlyList<DocumentoDetalle>> ListarAsync(int usuarioId)
    {
        var consulta = _contexto.Documentos.Where(d => !d.Eliminado);
        if (!await EsAdministradorAsync(usuarioId))
            consulta = consulta.Where(d => d.UsuarioId == usuarioId);

        var documentos = await consulta.OrderByDescending(d => d.FechaSubida).ThenByDescending(d => d.Id).ToListAsync();
        var nombres = await NombresDeAsync(documentos.Select(d => d.UsuarioId));
        return documentos.Select(d => Detalle(d, nombres.GetValueOrDefault(d.UsuarioId, "Usuario"))).ToList();
    }

    // RF-DOC-01: el registro queda consultable con todos sus datos (con las mismas reglas que la descarga).
    public async Task<ResultadoOperacionDocumento> ConsultarAsync(int documentoId, int usuarioId)
    {
        var (documento, rechazo) = await BuscarAutorizadoAsync(documentoId, usuarioId);
        if (rechazo is not null)
            return rechazo;

        var nombres = await NombresDeAsync([documento!.UsuarioId]);
        return ResultadoOperacionDocumento.Exito("Documento encontrado.",
            Detalle(documento, nombres.GetValueOrDefault(documento.UsuarioId, "Usuario")));
    }

    // RF-DOC-03: solo el dueño o un Administrador; cualquier otro usuario recibe SinPermiso.
    public async Task<ResultadoOperacionDocumento> DescargarAsync(int documentoId, int usuarioId)
    {
        var (documento, rechazo) = await BuscarAutorizadoAsync(documentoId, usuarioId);
        if (rechazo is not null)
            return rechazo;

        Stream? contenido;
        try
        {
            contenido = await _almacen.AbrirAsync(documento!.RutaAlmacenamiento);
        }
        catch (ErrorAlmacenDocumentos error)
        {
            return ResultadoOperacionDocumento.AlmacenNoDisponible(error.Message);
        }

        if (contenido is null)
            return ResultadoOperacionDocumento.AlmacenNoDisponible("El archivo de este documento no se encontró en el almacén.");

        return ResultadoOperacionDocumento.ExitoDescarga(new DocumentoDescarga(contenido, documento.Nombre, documento.Tipo));
    }

    // RF-DOC-04: eliminación lógica. Solo el dueño o un Administrador. El registro y el archivo siguen existiendo;
    // el documento deja de aparecer en el listado y ya no se descarga.
    public async Task<ResultadoOperacionDocumento> EliminarAsync(int documentoId, int usuarioId)
    {
        var (documento, rechazo) = await BuscarAutorizadoAsync(documentoId, usuarioId);
        if (rechazo is not null)
            return rechazo;

        documento!.MarcarEliminado(_reloj.AhoraUtc);
        await _contexto.SaveChangesAsync();

        var nombres = await NombresDeAsync([documento.UsuarioId]);
        return ResultadoOperacionDocumento.Exito("Documento eliminado.",
            Detalle(documento, nombres.GetValueOrDefault(documento.UsuarioId, "Usuario")));
    }

    // Un documento eliminado se trata como inexistente. Si existe, solo pasan su dueño y un Administrador.
    private async Task<(Documento? Documento, ResultadoOperacionDocumento? Rechazo)> BuscarAutorizadoAsync(
        int documentoId, int usuarioId)
    {
        var documento = await _contexto.Documentos.SingleOrDefaultAsync(d => d.Id == documentoId && !d.Eliminado);
        if (documento is null)
            return (null, ResultadoOperacionDocumento.NoEncontrado());

        if (documento.UsuarioId != usuarioId && !await EsAdministradorAsync(usuarioId))
            return (null, ResultadoOperacionDocumento.SinPermiso());

        return (documento, null);
    }

    private Task<bool> EsAdministradorAsync(int usuarioId) =>
        _contexto.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Rol == Rol.Administrador);

    private async Task<Dictionary<int, string>> NombresDeAsync(IEnumerable<int> usuarioIds)
    {
        var ids = usuarioIds.Distinct().ToList();
        return await _contexto.Usuarios.Where(u => ids.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Nombre);
    }

    private static DocumentoDetalle Detalle(Documento d, string subidoPor) =>
        new(d.Id, d.Nombre, d.Tipo, d.Tamano, d.FechaSubida, d.UsuarioId, subidoPor);
}
