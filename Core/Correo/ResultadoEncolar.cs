namespace Tracking_Tiger.Core.Correo;

// Resultado de intentar encolar un correo. Una entrada inválida se rechaza
// de forma controlada con un mensaje comprensible, sin lanzar excepciones (RD-07, RD-08).
public sealed record ResultadoEncolar(bool Exito, string Mensaje, int? CorreoId)
{
    public static ResultadoEncolar Aceptado(int correoId) =>
        new(true, "El correo quedó en la cola en estado pendiente.", correoId);

    // El correo quedó en el contexto; se guarda con el SaveChanges de quien llamó a Agregar.
    public static ResultadoEncolar Agregado() =>
        new(true, "El correo se guardará en la cola junto con la operación.", null);

    public static ResultadoEncolar Rechazado(string mensaje) =>
        new(false, mensaje, null);
}
