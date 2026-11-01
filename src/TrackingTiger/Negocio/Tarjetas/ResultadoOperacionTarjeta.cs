namespace Tracking_Tiger.Negocio.Tarjetas;

public enum EstadoOperacionTarjeta
{
    Exito,
    NoEncontrada,
    // La máquina de estados o una regla del negocio no permite la operación: la tarjeta no cambió.
    NoPermitida,
    // Falta el permiso aprobado y vigente de Gestión de permisos.
    SinPermiso,
    DatoInvalido
}

// Resultado de una operación del módulo Tarjetas. Transaccion solo viene al registrar una transacción.
public sealed record ResultadoOperacionTarjeta(EstadoOperacionTarjeta Estado, string Mensaje, TarjetaDetalle? Tarjeta,
    TransaccionListado? Transaccion = null)
{
    public static ResultadoOperacionTarjeta Exito(string mensaje, TarjetaDetalle tarjeta, TransaccionListado? transaccion = null) =>
        new(EstadoOperacionTarjeta.Exito, mensaje, tarjeta, transaccion);

    public static ResultadoOperacionTarjeta NoEncontrada() =>
        new(EstadoOperacionTarjeta.NoEncontrada, "No existe una tarjeta con ese id.", null);

    public static ResultadoOperacionTarjeta NoPermitida(string motivo) => new(EstadoOperacionTarjeta.NoPermitida, motivo, null);

    public static ResultadoOperacionTarjeta SinPermiso(string motivo) => new(EstadoOperacionTarjeta.SinPermiso, motivo, null);

    public static ResultadoOperacionTarjeta DatoInvalido(string mensaje) => new(EstadoOperacionTarjeta.DatoInvalido, mensaje, null);
}

// Datos de una tarjeta en las respuestas de la API. Nunca el número completo. Fechas en UTC.
public sealed record TarjetaDetalle(int Id, string Titular, string NumeroEnmascarado, decimal LimiteCredito,
    decimal Disponible, string Estado, DateTime FechaEmision)
{
    public static TarjetaDetalle De(Tarjeta tarjeta) =>
        new(tarjeta.Id, tarjeta.Titular, tarjeta.NumeroEnmascarado, tarjeta.LimiteCredito, tarjeta.Disponible,
            tarjeta.Estado.ToString(), tarjeta.FechaEmision);
}

public sealed record TransaccionListado(int Id, decimal Monto, string Comercio, DateTime Fecha, bool Aprobada,
    string? MotivoDeclinacion)
{
    public static TransaccionListado De(Transaccion t) =>
        new(t.Id, t.Monto, t.Comercio, t.Fecha, t.Aprobada, t.MotivoDeclinacion);
}

// Fila del historial de transiciones. EjecutadoPor es "Sistema" o el nombre del agente.
public sealed record HistorialListado(int Id, string EstadoAnterior, string EstadoNuevo, int? UsuarioId,
    string EjecutadoPor, string? Nota, DateTime Fecha);
