namespace Tracking_Tiger.Negocio.Tarjetas;

// Una transición aplicada a una tarjeta: de qué estado a cuál, quién la disparó, por qué y cuándo.
// Solo se agregan filas; nunca se editan ni se borran. Lo crea MaquinaEstadosTarjeta al aplicar una transición.
public class HistorialEstadoTarjeta
{
    public int Id { get; private set; }
    public int TarjetaId { get; private set; }
    public EstadoTarjeta EstadoAnterior { get; private set; }
    public EstadoTarjeta EstadoNuevo { get; private set; }

    // Agente que la ejecutó; null cuando la ejecutó el sistema.
    public int? UsuarioId { get; private set; }
    public string? Nota { get; private set; }
    public DateTime Fecha { get; private set; }

    // Solo para EF Core al leer filas.
    private HistorialEstadoTarjeta()
    {
    }

    internal static HistorialEstadoTarjeta Registrar(int tarjetaId, EstadoTarjeta anterior, EstadoTarjeta nuevo,
        int? usuarioId, string? nota, DateTime ahoraUtc) =>
        new()
        {
            TarjetaId = tarjetaId,
            EstadoAnterior = anterior,
            EstadoNuevo = nuevo,
            UsuarioId = usuarioId,
            Nota = nota,
            Fecha = ahoraUtc
        };
}
