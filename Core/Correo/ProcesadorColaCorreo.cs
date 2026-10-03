using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.Correo;

// Proceso independiente que envía los correos pendientes (RF-NOT-09).
// Corre fuera del flujo que creó cada correo. Solo camino feliz: sin reintentos ni estado Fallido.
public sealed class ProcesadorColaCorreo
{
    private readonly ContextoDatos _contexto;
    private readonly IEnviadorCorreo _enviador;
    private readonly IReloj _reloj;

    public ProcesadorColaCorreo(ContextoDatos contexto, IEnviadorCorreo enviador, IReloj reloj)
    {
        _contexto = contexto;
        _enviador = enviador;
        _reloj = reloj;
    }

    public async Task<ResultadoProcesarCola> ProcesarPendientesAsync()
    {
        var pendientes = await _contexto.CorreosEnCola
            .Where(c => c.Estado == EstadoCorreo.Pendiente)
            .OrderBy(c => c.Id)
            .ToListAsync();

        if (pendientes.Count == 0)
            return ResultadoProcesarCola.Correcto(0);

        // La configuración se lee aquí, al momento de enviar, no al iniciar la aplicación.
        var configuracion = ConfiguracionSmtp.Leer();
        if (!configuracion.Exito)
            return ResultadoProcesarCola.Error(configuracion.Mensaje!);

        await using (var sesion = await _enviador.AbrirSesionAsync(configuracion.Configuracion!))
        {
            foreach (var correo in pendientes)
            {
                await sesion.EnviarAsync(correo);
                correo.Estado = EstadoCorreo.Enviado;
                correo.FechaEnvio = _reloj.AhoraUtc;
            }
        }

        // El lote se guarda al final, en una sola operación.
        await _contexto.SaveChangesAsync();

        return ResultadoProcesarCola.Correcto(pendientes.Count);
    }
}
