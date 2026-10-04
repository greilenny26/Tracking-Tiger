namespace Tracking_Tiger.Core.Correo;

// Resultado de una ejecución del procesador de la cola, con los mensajes listos para mostrar.
// Avisos: un mensaje por cada correo que no se pudo enviar. Mensaje: el resumen final.
public sealed record ResultadoProcesarCola(bool Exito, int Enviados, int QuedanPendientes, IReadOnlyList<string> Avisos, string Mensaje)
{
    public static ResultadoProcesarCola Terminado(int enviados, int quedanPendientes, IReadOnlyList<string> avisos)
    {
        string mensaje;
        if (quedanPendientes > 0)
            mensaje = $"Correos enviados: {enviados}. Quedan pendientes: {quedanPendientes}.";
        else if (enviados == 0)
            mensaje = "No hay correos pendientes.";
        else
            mensaje = $"Correos enviados: {enviados}";

        return new(quedanPendientes == 0, enviados, quedanPendientes, avisos, mensaje);
    }

    public static ResultadoProcesarCola Error(string mensaje) => new(false, 0, 0, [], mensaje);
}
