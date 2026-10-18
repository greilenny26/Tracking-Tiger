using Microsoft.AspNetCore.Mvc;

namespace Tracking_Tiger.Core.Web;

// Respuesta 400 de los controladores cuando el cuerpo no se puede leer o no es válido (RD-07, RD-08).
// Reemplaza la respuesta por defecto de ASP.NET Core, que incluye en inglés el texto del analizador JSON
// (posición, ruta interna, tipo esperado).
public static class RespuestaSolicitudInvalida
{
    public static IActionResult Crear(ActionContext contexto)
    {
        // Los errores del analizador JSON usan claves "$..." (ruta JSON) o "" (cuerpo vacío o ilegible).
        var jsonMalFormado = contexto.ModelState.Keys.Any(clave => clave.Length == 0 || clave.StartsWith('$'));

        if (jsonMalFormado)
            return new BadRequestObjectResult(new
            {
                mensaje = "El cuerpo de la solicitud no es un JSON válido o no tiene el formato esperado."
            });

        var campos = contexto.ModelState
            .Where(entrada => entrada.Value?.Errors.Count > 0)
            .Select(entrada => entrada.Key)
            .ToList();

        return new BadRequestObjectResult(new
        {
            mensaje = "Los datos enviados no son válidos.",
            campos
        });
    }
}
