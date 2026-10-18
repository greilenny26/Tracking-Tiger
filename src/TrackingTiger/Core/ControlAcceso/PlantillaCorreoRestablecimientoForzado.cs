namespace Tracking_Tiger.Core.ControlAcceso;

// Asunto y cuerpo del correo de restablecimiento forzado por un Administrador (RF-CA-13). Texto plano, en español.
public static class PlantillaCorreoRestablecimientoForzado
{
    public const string Asunto = "Debes definir una nueva contraseña en Tracking Tiger";

    public static string Cuerpo(string nombre, string codigo) =>
        $"""
        Hola, {nombre}:

        Un Administrador restableció la contraseña de tu cuenta de Tracking Tiger. Tu contraseña anterior
        ya no sirve y tus sesiones abiertas se cerraron.
        Tu código para definir una contraseña nueva es:

        {codigo}

        Envía este código junto con tu contraseña nueva a POST /api/auth/restablecer.

        El código vence en {CodigoRecuperacion.Vigencia.TotalHours:0} hora y solo se puede usar una vez.

        Equipo de Tracking Tiger
        """;
}
