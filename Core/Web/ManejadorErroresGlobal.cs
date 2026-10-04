using Microsoft.AspNetCore.Diagnostics;

namespace Tracking_Tiger.Core.Web;

// Punto único que atrapa toda excepción no controlada de la API (RD-08).
// Al cliente solo le llega un mensaje genérico en JSON; la excepción real se registra en la consola del servidor.
// Corre dentro de la página de excepciones de desarrollo, así que esa página nunca llega al cliente.
public sealed class ManejadorErroresGlobal : IExceptionHandler
{
    private readonly ILogger<ManejadorErroresGlobal> _registro;

    public ManejadorErroresGlobal(ILogger<ManejadorErroresGlobal> registro)
    {
        _registro = registro;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception excepcion, CancellationToken cancelacion)
    {
        // Petición mal formada detectada por el servidor (por ejemplo, JSON inválido en un endpoint mínimo).
        if (excepcion is BadHttpRequestException solicitudInvalida)
        {
            _registro.LogWarning(excepcion, "Solicitud inválida en {Metodo} {Ruta}", contexto.Request.Method, contexto.Request.Path);
            await EscribirAsync(contexto, solicitudInvalida.StatusCode,
                "La solicitud no es válida. Revisa que el cuerpo sea un JSON bien formado.", cancelacion);
            return true;
        }

        _registro.LogError(excepcion, "Excepción no controlada en {Metodo} {Ruta}", contexto.Request.Method, contexto.Request.Path);
        await EscribirAsync(contexto, StatusCodes.Status500InternalServerError,
            "Ocurrió un error inesperado. Intenta de nuevo más tarde.", cancelacion);
        return true;
    }

    private static async Task EscribirAsync(HttpContext contexto, int codigo, string mensaje, CancellationToken cancelacion)
    {
        contexto.Response.StatusCode = codigo;
        await contexto.Response.WriteAsJsonAsync(new { mensaje }, cancelacion);
    }
}
