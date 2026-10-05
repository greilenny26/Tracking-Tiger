namespace Tracking_Tiger.Core.ControlAcceso;

// Asunto y cuerpo del correo de recuperación de contraseña (RF-CA-10). Texto plano, en español.
public static class PlantillaCorreoRecuperacion
{
    public const string Asunto = "Recupera tu contraseña de Tracking Tiger";

    public static string Cuerpo(string nombre, string codigo) =>
        $"""
        Hola, {nombre}:

        Recibimos una solicitud para recuperar la contraseña de tu cuenta de Tracking Tiger.
        Tu código de recuperación es:

        {codigo}

        Para definir una contraseña nueva, envía este código junto con tu correo y tu contraseña nueva
        a POST /api/auth/restablecer.

        El código vence en {CodigoRecuperacion.Vigencia.TotalHours:0} hora y solo se puede usar una vez.
        Si no pediste recuperar tu contraseña, ignora este correo: tu contraseña actual sigue funcionando.

        Equipo de Tracking Tiger
        """;
}
