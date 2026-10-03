using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.Correo;

// Proceso independiente que envía los correos pendientes (RF-NOT-09).
// Corre fuera del flujo que creó cada correo. Ejecutarlo dos veces no duplica envíos (RF-NOT-12):
// cada fila se vuelve a consultar justo antes de enviarla y se guarda como Enviado apenas sale.
// Sin reintentos ni estado Fallido todavía.
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

        var enviados = 0;
        await using (var sesion = await _enviador.AbrirSesionAsync(configuracion.Configuracion!))
        {
            foreach (var correo in pendientes)
            {
                // Otra ejecución pudo haberlo enviado desde que se cargó la lista.
                // Si la fila ya no existe, ReloadAsync la desvincula y conserva el estado viejo en memoria.
                var entrada = _contexto.Entry(correo);
                await entrada.ReloadAsync();
                if (entrada.State == EntityState.Detached || correo.Estado != EstadoCorreo.Pendiente)
                    continue;

                await sesion.EnviarAsync(correo);

                correo.Estado = EstadoCorreo.Enviado;
                correo.FechaEnvio = _reloj.AhoraUtc;
                await _contexto.SaveChangesAsync();
                enviados++;
            }
        }

        return ResultadoProcesarCola.Correcto(enviados);
    }
}
