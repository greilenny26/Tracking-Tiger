namespace Tracking_Tiger.Core.Comun;

// Resultado de validar una entrada externa. Un dato inválido se rechaza de forma
// controlada con un mensaje comprensible, sin lanzar excepciones (RD-07).
public sealed record ResultadoValidacion(bool Valido, string? Mensaje)
{
    public static ResultadoValidacion Correcto() => new(true, null);

    public static ResultadoValidacion Rechazado(string mensaje) => new(false, mensaje);
}
