namespace Tracking_Tiger.Core.Comun;

// Reloj único del sistema (RD-11). Toda fecha y hora del proyecto se obtiene de aquí, en UTC.
public interface IReloj
{
    DateTime AhoraUtc { get; }
}
