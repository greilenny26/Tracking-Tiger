using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.Correo;

// Proceso independiente que envía los correos pendientes (RF-NOT-09).
// Corre fuera del flujo que creó cada correo. Ejecutarlo dos veces no duplica envíos (RF-NOT-12):
// cada fila se vuelve a consultar justo antes de enviarla y se guarda como Enviado apenas sale.
// Cada intento de entrega suma uno a Intentos. Un correo que falla queda Pendiente hasta MaximoIntentos y
// entonces pasa a Fallido, con su último error (RF-NOT-10). Si no se puede ni conectar, cuenta como un
// intento de cada pendiente. Si falta configuración no se intenta nada y no se suma ningún intento.
public sealed class ProcesadorColaCorreo
{
    // RF-NOT-10: declarado también en el README.
    public const int MaximoIntentos = 3;

    // Igual al largo de la columna UltimoError en ContextoDatos.
    private const int LargoMaximoError = 500;

    private readonly Func<string, string?>? _leerVariable;
    private readonly ContextoDatos _contexto;
    private readonly IEnviadorCorreo _enviador;
    private readonly IReloj _reloj;

    // leerVariable permite a las pruebas dar la configuración SMTP; por defecto, el entorno del proceso.
    public ProcesadorColaCorreo(ContextoDatos contexto, IEnviadorCorreo enviador, IReloj reloj,
        Func<string, string?>? leerVariable = null)
    {
        _leerVariable = leerVariable;
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
            return ResultadoProcesarCola.Terminado(0, 0, []);

        // La configuración se lee aquí, al momento de enviar, no al iniciar la aplicación.
        // Si falta alguna variable no se toca ninguna fila: todas siguen Pendiente.
        var configuracion = ConfiguracionSmtp.Leer(_leerVariable);
        if (!configuracion.Exito)
            return ResultadoProcesarCola.Error(configuracion.Mensaje!);

        var avisos = new List<string>();
        var enviados = 0;
        var omitidos = 0;
        var fallidos = 0;

        ISesionCorreo sesion;
        try
        {
            sesion = await _enviador.AbrirSesionAsync(configuracion.Configuracion!);
        }
        catch (ErrorEnvioCorreo error)
        {
            avisos.Add(error.Message);
            fallidos = pendientes.Count(correo => RegistrarFallo(correo, error.Message));
            await _contexto.SaveChangesAsync();
            return ResultadoProcesarCola.Terminado(0, pendientes.Count - fallidos, avisos, fallidos);
        }

        await using (sesion)
        {
            foreach (var correo in pendientes)
            {
                // Otra ejecución pudo haberlo enviado desde que se cargó la lista.
                // Si la fila ya no existe, ReloadAsync la desvincula y conserva el estado viejo en memoria.
                var entrada = _contexto.Entry(correo);
                await entrada.ReloadAsync();
                if (entrada.State == EntityState.Detached || correo.Estado != EstadoCorreo.Pendiente)
                {
                    omitidos++;
                    continue;
                }

                try
                {
                    await sesion.EnviarAsync(correo);
                }
                catch (ErrorEnvioCorreo error)
                {
                    var agotado = RegistrarFallo(correo, error.Message);
                    await _contexto.SaveChangesAsync();
                    if (agotado)
                        fallidos++;
                    avisos.Add($"Correo {correo.Id}: {error.Message} " +
                        (agotado ? "Queda fallido." : $"Queda pendiente (intento {correo.Intentos} de {MaximoIntentos})."));
                    if (error.DetieneElLote)
                    {
                        avisos.Add("Se detuvo el envío; los correos restantes quedan pendientes.");
                        break;
                    }
                    continue;
                }

                correo.Intentos++;
                correo.Estado = EstadoCorreo.Enviado;
                correo.FechaEnvio = _reloj.AhoraUtc;
                await _contexto.SaveChangesAsync();
                enviados++;
            }
        }

        return ResultadoProcesarCola.Terminado(enviados, pendientes.Count - enviados - omitidos - fallidos, avisos, fallidos);
    }

    // RF-NOT-10: anota el intento fallido; devuelve true si con él se agotaron los intentos.
    private static bool RegistrarFallo(CorreoEnCola correo, string error)
    {
        correo.Intentos++;
        correo.UltimoError = error.Length > LargoMaximoError ? error[..LargoMaximoError] : error;
        if (correo.Intentos < MaximoIntentos)
            return false;

        correo.Estado = EstadoCorreo.Fallido;
        return true;
    }
}
