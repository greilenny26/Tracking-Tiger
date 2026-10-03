namespace Tracking_Tiger.Core.Correo;

// Resultado de una ejecución del procesador de la cola, con el mensaje listo para mostrar.
public sealed record ResultadoProcesarCola(bool Exito, int Enviados, string Mensaje)
{
    public static ResultadoProcesarCola Correcto(int enviados) =>
        new(true, enviados, enviados == 0 ? "No hay correos pendientes." : $"Correos enviados: {enviados}");

    public static ResultadoProcesarCola Error(string mensaje) => new(false, 0, mensaje);
}
