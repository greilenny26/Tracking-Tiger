namespace Tracking_Tiger.Core.Correo;

// Resultado de intentar encolar un correo. Una entrada inválida se rechaza
// de forma controlada con un mensaje comprensible, sin lanzar excepciones (RD-07, RD-08).
public sealed record ResultadoEncolar(bool Exito, string Mensaje, int? CorreoId)
{
    public static ResultadoEncolar Aceptado(int correoId) =>
        new(true, "El correo quedó en la cola en estado pendiente.", correoId);

    public static ResultadoEncolar Rechazado(string mensaje) =>
        new(false, mensaje, null);
}
