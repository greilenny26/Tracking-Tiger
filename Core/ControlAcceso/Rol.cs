namespace Tracking_Tiger.Core.ControlAcceso;

// Roles del sistema (RF-CA-04): único lugar donde se declaran. Todo usuario tiene exactamente uno.
// El identificador va sin tilde (Estandar); en los mensajes al usuario se muestra con NombreVisible().
public enum Rol
{
    Administrador,
    Estandar
}

public static class RolExtensiones
{
    // Nombre en español para mensajes al usuario: "Administrador" o "Estándar".
    public static string NombreVisible(this Rol rol) => rol switch
    {
        Rol.Administrador => "Administrador",
        Rol.Estandar => "Estándar",
        _ => rol.ToString()
    };
}
