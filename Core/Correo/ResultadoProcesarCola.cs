namespace Tracking_Tiger.Core.Correo;

// Resultado de una ejecución del procesador de la cola.
public sealed record ResultadoProcesarCola(bool Exito, int Enviados, string? Mensaje)
{
    public static ResultadoProcesarCola Correcto(int enviados) => new(true, enviados, null);

    public static ResultadoProcesarCola Error(string mensaje) => new(false, 0, mensaje);
}
