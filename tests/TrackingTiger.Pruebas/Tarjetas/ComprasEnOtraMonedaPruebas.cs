using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.Permisos;
using Tracking_Tiger.Core.Persistencia;
using Tracking_Tiger.Negocio.Tarjetas;

namespace TrackingTiger.Pruebas.Tarjetas;

// Compras en otra moneda: el monto se convierte a DOP con la tasa de la API antes de compararlo con el crédito.
public sealed class ComprasEnOtraMonedaPruebas : IDisposable
{
    private static readonly DateTime Ahora = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly SqliteConnection _conexion;
    private readonly ContextoDatos _contexto;

    public ComprasEnOtraMonedaPruebas()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();
        var opciones = new DbContextOptionsBuilder<ContextoDatos>().UseSqlite(_conexion).Options;
        _contexto = new ContextoDatos(opciones);
        _contexto.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _contexto.Dispose();
        _conexion.Dispose();
    }

    private ServicioTarjetas CrearServicio(IProveedorTasaCambio tasas)
    {
        var reloj = new RelojFijo(Ahora);
        return new ServicioTarjetas(_contexto, reloj, new ServicioSolicitudesPermiso(_contexto, reloj), tasas);
    }

    private static async Task<int> EmitirTarjetaAsync(ServicioTarjetas servicio, decimal limite)
    {
        var emision = await servicio.EmitirAsync("Ana Pérez", "1234", limite);
        return emision.Tarjeta!.Id;
    }

    [Fact]
    public async Task RegistrarTransaccionAsync_EnUsd_ConvierteADopYGuardaLoOriginal()
    {
        // Preparar
        var servicio = CrearServicio(new TasaCambioFalsa(tasa: 60m));
        var tarjeta = await EmitirTarjetaAsync(servicio, 10_000m);

        // Actuar
        var resultado = await servicio.RegistrarTransaccionAsync(tarjeta, 100m, "Tienda en línea", "USD");

        // Verificar
        Assert.Equal(EstadoOperacionTarjeta.Exito, resultado.Estado);
        Assert.True(resultado.Transaccion!.Aprobada);
        Assert.Equal(6_000m, resultado.Transaccion.Monto);
        Assert.Equal(100m, resultado.Transaccion.MontoOriginal);
        Assert.Equal("USD", resultado.Transaccion.MonedaOriginal);
        Assert.Equal(60m, resultado.Transaccion.TasaCambio);
        Assert.Equal(4_000m, resultado.Tarjeta!.Disponible);
    }

    // El monto cabe en el crédito sin convertir (200 < 10 000), pero en pesos no: la decisión la cambia la tasa real.
    [Fact]
    public async Task RegistrarTransaccionAsync_EnUsdQueConvertidoSuperaElCredito_SeDeclina()
    {
        // Preparar
        var servicio = CrearServicio(new TasaCambioFalsa(tasa: 60m));
        var tarjeta = await EmitirTarjetaAsync(servicio, 10_000m);

        // Actuar
        var resultado = await servicio.RegistrarTransaccionAsync(tarjeta, 200m, "Tienda en línea", "USD");

        // Verificar
        Assert.False(resultado.Transaccion!.Aprobada);
        Assert.Equal(12_000m, resultado.Transaccion.Monto);
        Assert.Equal(10_000m, resultado.Tarjeta!.Disponible);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("dop")]
    public async Task RegistrarTransaccionAsync_EnDop_NoConsultaLaApi(string? moneda)
    {
        // Preparar
        var tasas = new TasaCambioFalsa();
        var servicio = CrearServicio(tasas);
        var tarjeta = await EmitirTarjetaAsync(servicio, 1_000m);

        // Actuar
        var resultado = await servicio.RegistrarTransaccionAsync(tarjeta, 100m, "Farmacia", moneda);

        // Verificar
        Assert.True(resultado.Transaccion!.Aprobada);
        Assert.Equal(100m, resultado.Transaccion.Monto);
        Assert.Null(resultado.Transaccion.MonedaOriginal);
        Assert.Equal(0, tasas.Consultas);
    }

    [Fact]
    public async Task RegistrarTransaccionAsync_ConMonedaNoAdmitida_SeRechazaSinConsultarLaApi()
    {
        // Preparar
        var tasas = new TasaCambioFalsa();
        var servicio = CrearServicio(tasas);
        var tarjeta = await EmitirTarjetaAsync(servicio, 1_000m);

        // Actuar
        var resultado = await servicio.RegistrarTransaccionAsync(tarjeta, 100m, "Farmacia", "XYZ");

        // Verificar
        Assert.Equal(EstadoOperacionTarjeta.DatoInvalido, resultado.Estado);
        Assert.Equal(0, tasas.Consultas);
    }

    [Fact]
    public async Task RegistrarTransaccionAsync_SinTasaDisponible_SeRechazaSinRegistrarNadaNiTocarElCredito()
    {
        // Preparar
        var servicio = CrearServicio(new TasaCambioFalsa(error: "Servicio no disponible."));
        var tarjeta = await EmitirTarjetaAsync(servicio, 10_000m);

        // Actuar
        var resultado = await servicio.RegistrarTransaccionAsync(tarjeta, 10m, "Tienda en línea", "USD");

        // Verificar
        Assert.Equal(EstadoOperacionTarjeta.ServicioNoDisponible, resultado.Estado);
        Assert.Equal("Servicio no disponible.", resultado.Mensaje);
        Assert.Equal(0, await _contexto.Set<Transaccion>().CountAsync());
        Assert.Equal(10_000m, (await servicio.ConsultarAsync(tarjeta)).Tarjeta!.Disponible);
    }

    // Una falla de la API no es una declinación: por muchas que haya, no dispara el Bloqueo Suave por ráfaga.
    [Fact]
    public async Task RegistrarTransaccionAsync_ConVariasFallasDeLaApi_LaTarjetaSigueActiva()
    {
        // Preparar
        var servicio = CrearServicio(new TasaCambioFalsa(error: "Servicio no disponible."));
        var tarjeta = await EmitirTarjetaAsync(servicio, 10_000m);

        // Actuar
        for (var intento = 0; intento < ServicioTarjetas.DeclinacionesParaRafaga; intento++)
            await servicio.RegistrarTransaccionAsync(tarjeta, 10m, "Tienda en línea", "USD");

        // Verificar
        Assert.Equal(nameof(EstadoTarjeta.Activa), (await servicio.ConsultarAsync(tarjeta)).Tarjeta!.Estado);
    }

    private sealed class RelojFijo(DateTime ahoraUtc) : IReloj
    {
        public DateTime AhoraUtc => ahoraUtc;
    }
}
