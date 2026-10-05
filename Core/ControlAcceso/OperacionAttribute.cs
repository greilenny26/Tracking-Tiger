namespace Tracking_Tiger.Core.ControlAcceso;

// Etiqueta un endpoint con el nombre de su operación en CatalogoOperaciones (RF-CA-05).
// Solo nombra la operación: la exigencia de acceso se lee siempre del catálogo, nunca de aquí.
[AttributeUsage(AttributeTargets.Method)]
public sealed class OperacionAttribute : Attribute
{
    public string Nombre { get; }

    public OperacionAttribute(string nombre) => Nombre = nombre;
}
