namespace Tracking_Tiger.Core.Comun;

// Implementación de IReloj que lee la hora del sistema.
// Es el ÚNICO lugar del proyecto que consulta la hora del sistema.
public sealed class RelojSistema : IReloj
{
    public DateTime AhoraUtc => DateTime.UtcNow;
}
