namespace Tracking_Tiger.Core.Notificaciones;

// Pieza 4 del Core: aviso que su destinatario ve dentro del sistema (RF-NOT-04, RF-NOT-06).
// Todas las fechas están en UTC (RD-11).
public class Notificacion
{
    public const int LargoMaximoTipo = 50;
    public const int LargoMaximoMensaje = 1000;

    public int Id { get; private set; }

    // Quién la recibe. Solo esa persona la puede consultar o marcar.
    public int DestinatarioId { get; private set; }

    // Qué ocurrió. Es texto para que cada módulo declare sus tipos sin que el Core los conozca (RD-03).
    public string Tipo { get; private set; } = string.Empty;

    public string Mensaje { get; private set; } = string.Empty;

    public bool Leida { get; private set; }

    public DateTime Fecha { get; private set; }

    // Solo para EF Core al leer filas. Toda notificación nueva se crea con Crear.
    private Notificacion()
    {
    }

    // Toda notificación nace sin leer.
    public static Notificacion Crear(int destinatarioId, string tipo, string mensaje, DateTime ahoraUtc) =>
        new()
        {
            DestinatarioId = destinatarioId,
            Tipo = tipo,
            Mensaje = mensaje,
            Leida = false,
            Fecha = ahoraUtc
        };

    // RF-NOT-05: marcarla otra vez no cambia nada.
    public void MarcarLeida() => Leida = true;
}
