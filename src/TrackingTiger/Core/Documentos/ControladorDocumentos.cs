using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Tracking_Tiger.Core.ControlAcceso;

namespace Tracking_Tiger.Core.Documentos;

// Endpoints del Manejador de documentos. Solo traducen HTTP ↔ servicio (RD-02).
// Todas exigen sesión (CatalogoOperaciones); quién puede tocar cada documento lo decide ServicioDocumentos (RD-06).
[ApiController]
[Route("api/documentos")]
public sealed class ControladorDocumentos : ControllerBase
{
    private readonly ServicioDocumentos _servicio;

    public ControladorDocumentos(ServicioDocumentos servicio)
    {
        _servicio = servicio;
    }

    // RF-DOC-01: multipart/form-data con el archivo en el campo "archivo".
    // 201 con el registro; 400 si falta el archivo, está vacío o es muy grande; 503 si el almacén no responde.
    [Operacion(CatalogoOperaciones.SubirDocumento)]
    [HttpPost]
    public async Task<IActionResult> Subir([FromForm] IFormFile? archivo)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
            return SesionInvalida();

        await using var contenido = archivo?.OpenReadStream();
        var resultado = await _servicio.SubirAsync(usuarioId, archivo?.FileName, archivo?.ContentType, archivo?.Length ?? 0, contenido);
        return resultado.Estado == EstadoOperacionDocumento.Exito
            ? StatusCode(StatusCodes.Status201Created, resultado.Documento)
            : Responder(resultado);
    }

    // RF-DOC-02: sin eliminados; el Administrador ve todos, los demás solo los suyos.
    [Operacion(CatalogoOperaciones.ListarDocumentos)]
    [HttpGet]
    public async Task<IActionResult> Listar() =>
        TryObtenerUsuarioId(out var usuarioId) ? Ok(await _servicio.ListarAsync(usuarioId)) : SesionInvalida();

    // RF-DOC-01: 200 con el registro; 403 si no es el dueño ni Administrador; 404 si no existe o está eliminado.
    [Operacion(CatalogoOperaciones.ConsultarDocumento)]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Consultar(int id) =>
        TryObtenerUsuarioId(out var usuarioId) ? Responder(await _servicio.ConsultarAsync(id, usuarioId)) : SesionInvalida();

    // RF-DOC-03: 200 con el archivo; 403 si no es el dueño ni Administrador; 404 si no existe o está eliminado.
    [Operacion(CatalogoOperaciones.DescargarDocumento)]
    [HttpGet("{id:int}/descargar")]
    public async Task<IActionResult> Descargar(int id)
    {
        if (!TryObtenerUsuarioId(out var usuarioId))
            return SesionInvalida();

        var resultado = await _servicio.DescargarAsync(id, usuarioId);
        if (resultado.Estado != EstadoOperacionDocumento.Exito)
            return Responder(resultado);

        // File() cierra el stream al terminar de enviarlo.
        var descarga = resultado.Descarga!;
        return File(descarga.Contenido, descarga.Tipo, descarga.Nombre);
    }

    // RF-DOC-04: eliminación lógica. 200 con el registro; 403 si no es el dueño ni Administrador; 404 si no existe.
    [Operacion(CatalogoOperaciones.EliminarDocumento)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id) =>
        TryObtenerUsuarioId(out var usuarioId) ? Responder(await _servicio.EliminarAsync(id, usuarioId)) : SesionInvalida();

    private IActionResult Responder(ResultadoOperacionDocumento resultado) => resultado.Estado switch
    {
        EstadoOperacionDocumento.Exito => Ok(new { mensaje = resultado.Mensaje, documento = resultado.Documento }),
        EstadoOperacionDocumento.NoEncontrado => NotFound(new { mensaje = resultado.Mensaje }),
        EstadoOperacionDocumento.SinPermiso => StatusCode(StatusCodes.Status403Forbidden, new { mensaje = resultado.Mensaje }),
        EstadoOperacionDocumento.AlmacenNoDisponible => StatusCode(StatusCodes.Status503ServiceUnavailable, new { mensaje = resultado.Mensaje }),
        _ => BadRequest(new { mensaje = resultado.Mensaje })
    };

    private UnauthorizedObjectResult SesionInvalida() => Unauthorized(new { mensaje = "Sesión no válida o vencida." });

    // El filtro ya garantizó una sesión válida; el id sale de esa sesión (claim del servidor).
    private bool TryObtenerUsuarioId(out int usuarioId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);
}
