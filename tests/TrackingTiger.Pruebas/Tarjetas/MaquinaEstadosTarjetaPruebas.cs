using Tracking_Tiger.Negocio.Tarjetas;

namespace TrackingTiger.Pruebas.Tarjetas;

// Pruebas de la máquina de estados de la Tarjeta (RD-12): sin base de datos y sin levantar la aplicación.
// Una prueba por cada fila de las tablas de docs/maquina-de-estados.md, más el estado terminal y las condiciones.
// Cada prueba crea su propia tarjeta: no comparten estado y pasan en cualquier orden.
public sealed class MaquinaEstadosTarjetaPruebas
{
    private const int Agente = 7;
    private const string Nota = "El cliente confirmó por teléfono.";
    private static readonly DateTime Ahora = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    // ---------- Transiciones permitidas ----------

    // Permitida 1: Activa → BloqueoSuave (sistema)
    [Fact]
    public void IntentarTransicion_ActivaABloqueoSuavePorElSistema_SePermiteYQuedaEnElHistorialSinUsuario()
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.Activa);

        // Actuar
        var resultado = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.BloqueoSuave, agenteId: null, Ahora, "Ráfaga.");

        // Verificar
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoTarjeta.BloqueoSuave, tarjeta.Estado);
        Assert.Null(resultado.Historial!.UsuarioId);
    }

    // Permitida 2: BloqueoSuave → Activa (agente)
    [Fact]
    public void IntentarTransicion_BloqueoSuaveAActivaPorUnAgente_SePermite()
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.BloqueoSuave);

        // Actuar
        var resultado = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.Activa, Agente, Ahora);

        // Verificar
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoTarjeta.Activa, tarjeta.Estado);
    }

    // Permitida 3: BloqueoSuave → BloqueoTotal (agente, con nota)
    [Fact]
    public void IntentarTransicion_BloqueoSuaveABloqueoTotalPorUnAgenteConNota_SePermite()
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.BloqueoSuave);

        // Actuar
        var resultado = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.BloqueoTotal, Agente, Ahora, Nota);

        // Verificar
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoTarjeta.BloqueoTotal, tarjeta.Estado);
    }

    // Permitida 4: BloqueoTotal → Activa (agente, con nota; el permiso lo exige ServicioTarjetas)
    [Fact]
    public void IntentarTransicion_BloqueoTotalAActivaPorUnAgenteConNota_SePermite()
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.BloqueoTotal);

        // Actuar
        var resultado = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.Activa, Agente, Ahora, Nota);

        // Verificar
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoTarjeta.Activa, tarjeta.Estado);
    }

    // Permitida 5: Activa → Cancelada
    [Fact]
    public void IntentarTransicion_ActivaACanceladaPorUnAgenteConNota_SePermite()
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.Activa);

        // Actuar
        var resultado = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.Cancelada, Agente, Ahora, "Robo.");

        // Verificar
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoTarjeta.Cancelada, tarjeta.Estado);
    }

    // Permitida 6: BloqueoSuave → Cancelada
    [Fact]
    public void IntentarTransicion_BloqueoSuaveACanceladaPorUnAgenteConNota_SePermite()
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.BloqueoSuave);

        // Actuar
        var resultado = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.Cancelada, Agente, Ahora, "Robo.");

        // Verificar
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoTarjeta.Cancelada, tarjeta.Estado);
    }

    // Permitida 7: BloqueoTotal → Cancelada
    [Fact]
    public void IntentarTransicion_BloqueoTotalACanceladaPorUnAgenteConNota_SePermite()
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.BloqueoTotal);

        // Actuar
        var resultado = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.Cancelada, Agente, Ahora, "Robo.");

        // Verificar
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoTarjeta.Cancelada, tarjeta.Estado);
    }

    // ---------- Transiciones prohibidas (RF-NEG-04): se intentan con nota, por un agente y por el sistema ----------

    // Prohibida 1: Activa → BloqueoTotal
    [Fact]
    public void IntentarTransicion_ActivaABloqueoTotal_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.Activa);

        // Actuar
        var porAgente = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.BloqueoTotal, Agente, Ahora, Nota);
        var porSistema = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.BloqueoTotal, agenteId: null, Ahora, Nota);

        // Verificar
        Assert.False(porAgente.Exito);
        Assert.False(porSistema.Exito);
        Assert.Equal(EstadoTarjeta.Activa, tarjeta.Estado);
    }

    // Prohibida 2: BloqueoTotal → BloqueoSuave
    [Fact]
    public void IntentarTransicion_BloqueoTotalABloqueoSuave_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.BloqueoTotal);

        // Actuar
        var porAgente = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.BloqueoSuave, Agente, Ahora, Nota);
        var porSistema = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.BloqueoSuave, agenteId: null, Ahora, Nota);

        // Verificar
        Assert.False(porAgente.Exito);
        Assert.False(porSistema.Exito);
        Assert.Equal(EstadoTarjeta.BloqueoTotal, tarjeta.Estado);
    }

    // Prohibida 3: Cancelada → Activa
    [Fact]
    public void IntentarTransicion_CanceladaAActiva_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.Cancelada);

        // Actuar
        var porAgente = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.Activa, Agente, Ahora, Nota);
        var porSistema = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.Activa, agenteId: null, Ahora, Nota);

        // Verificar
        Assert.False(porAgente.Exito);
        Assert.False(porSistema.Exito);
        Assert.Equal(EstadoTarjeta.Cancelada, tarjeta.Estado);
    }

    // ---------- Estado terminal (RF-NEG-05) ----------

    // De Cancelada no se sale hacia ningún estado, lo ejecute el sistema o un agente con nota.
    [Theory]
    [InlineData(EstadoTarjeta.Activa)]
    [InlineData(EstadoTarjeta.BloqueoSuave)]
    [InlineData(EstadoTarjeta.BloqueoTotal)]
    public void IntentarTransicion_DesdeCancelada_NoSaleANingunEstado(EstadoTarjeta hacia)
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.Cancelada);

        // Actuar
        var porAgente = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, hacia, Agente, Ahora, Nota);
        var porSistema = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, hacia, agenteId: null, Ahora, Nota);

        // Verificar
        Assert.False(porAgente.Exito);
        Assert.False(porSistema.Exito);
        Assert.Equal(EstadoTarjeta.Cancelada, tarjeta.Estado);
    }

    // RF-NEG-05: ninguna transición permitida parte de Cancelada.
    [Fact]
    public void EsTerminal_Cancelada_NingunaTransicionParteDeEse()
    {
        // Preparar
        var estado = EstadoTarjeta.Cancelada;

        // Actuar
        var esTerminal = MaquinaEstadosTarjeta.EsTerminal(estado);

        // Verificar
        Assert.True(esTerminal);
        Assert.DoesNotContain(MaquinaEstadosTarjeta.Permitidas, t => t.Desde == estado);
    }

    // ---------- Condiciones de las transiciones ----------

    // Condición de la permitida 1: solo el sistema pone el Bloqueo Suave; un agente no puede hacerlo a mano.
    [Fact]
    public void IntentarTransicion_ActivaABloqueoSuavePorUnAgente_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.Activa);

        // Actuar
        var resultado = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.BloqueoSuave, Agente, Ahora, Nota);

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoTarjeta.Activa, tarjeta.Estado);
    }

    // Condición de las permitidas 2 a 7: las ejecuta un agente, nunca el sistema por su cuenta.
    [Fact]
    public void IntentarTransicion_BloqueoSuaveAActivaSinAgente_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.BloqueoSuave);

        // Actuar
        var resultado = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.Activa, agenteId: null, Ahora);

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoTarjeta.BloqueoSuave, tarjeta.Estado);
    }

    // Condición de la permitida 3: no reconocer la actividad exige una nota.
    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void IntentarTransicion_BloqueoSuaveABloqueoTotalSinNota_SeRechazaYElEstadoNoCambia(string? nota)
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.BloqueoSuave);

        // Actuar
        var resultado = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.BloqueoTotal, Agente, Ahora, nota);

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoTarjeta.BloqueoSuave, tarjeta.Estado);
    }

    // Condición de la permitida 4: levantar un Bloqueo Total exige una nota.
    [Fact]
    public void IntentarTransicion_BloqueoTotalAActivaSinNota_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.BloqueoTotal);

        // Actuar
        var resultado = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.Activa, Agente, Ahora, nota: null);

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoTarjeta.BloqueoTotal, tarjeta.Estado);
    }

    // Reconocer la actividad (origen exigido BloqueoSuave) no saca a una tarjeta de BloqueoTotal.
    [Fact]
    public void IntentarTransicion_ConOrigenExigidoDistintoDelActual_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var tarjeta = TarjetaEn(EstadoTarjeta.BloqueoTotal);

        // Actuar
        var resultado = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.Activa, Agente, Ahora, Nota,
            desdeEsperado: EstadoTarjeta.BloqueoSuave);

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoTarjeta.BloqueoTotal, tarjeta.Estado);
    }

    // Lleva una tarjeta nueva al estado pedido usando solo transiciones permitidas.
    private static Tarjeta TarjetaEn(EstadoTarjeta estado)
    {
        var tarjeta = Tarjeta.Emitir("Ana Pérez", "1234", 1000m, Ahora);
        switch (estado)
        {
            case EstadoTarjeta.BloqueoSuave:
                Mover(tarjeta, EstadoTarjeta.BloqueoSuave, agenteId: null);
                break;
            case EstadoTarjeta.BloqueoTotal:
                Mover(tarjeta, EstadoTarjeta.BloqueoSuave, agenteId: null);
                Mover(tarjeta, EstadoTarjeta.BloqueoTotal, Agente);
                break;
            case EstadoTarjeta.Cancelada:
                Mover(tarjeta, EstadoTarjeta.Cancelada, Agente);
                break;
        }

        Assert.Equal(estado, tarjeta.Estado);
        return tarjeta;
    }

    private static void Mover(Tarjeta tarjeta, EstadoTarjeta hacia, int? agenteId) =>
        Assert.True(MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, hacia, agenteId, Ahora, Nota).Exito);
}
