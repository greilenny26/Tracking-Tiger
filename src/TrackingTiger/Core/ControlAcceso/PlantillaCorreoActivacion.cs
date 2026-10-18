namespace Tracking_Tiger.Core.ControlAcceso;

// Asunto y cuerpo del correo de activación (RF-CA-15). Texto plano, en español.
public static class PlantillaCorreoActivacion
{
    public const string Asunto = "Activa tu cuenta de Tracking Tiger";

    public static string Cuerpo(string nombre, string enlace) =>
        $"""
        Hola, {nombre}:

        Tu cuenta de Tracking Tiger se creó, pero todavía no está activa.
        Para activarla, abre este enlace:

        {enlace}

        El enlace vence en {TokenActivacion.Vigencia.TotalHours:0} horas y solo se puede usar una vez.
        Si no creaste esta cuenta, ignora este correo.

        Equipo de Tracking Tiger
        """;
}
