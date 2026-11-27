using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Correo;

namespace TrackingTiger.Pruebas.Correo;

// Criterios C-02 a C-09: reintentos, estado fallido, no duplicar y consulta de la cola.
// El servidor de correo se sustituye por uno falso que se puede apagar.
public sealed class ColaCorreoPruebas : IDisposable
{
    private readonly BaseEnMemoria _base = new();
    private readonly ServidorFalso _servidor = new();
    private readonly Dictionary<string, string?> _variables = new()
    {
        [ConfiguracionSmtp.VariableHost] = "servidor.prueba",
        [ConfiguracionSmtp.VariableUsuario] = "usuario@prueba.local",
        [ConfiguracionSmtp.VariableClave] = "valor-de-prueba"
    };

    public void Dispose() => _base.Dispose();

    // C-02 y C-09 (RF-NOT-09, RF-NOT-12)
    [Fact]
    public async Task Procesar_DosVeces_EnviaUnaSolaVez()
    {
        // Preparar
        await EncolarAsync();

        // Actuar
        await ProcesarAsync();
        await ProcesarAsync();

        // Verificar
        var correo = await LeerAsync();
        Assert.Equal(1, _servidor.Enviados);
        Assert.Equal(EstadoCorreo.Enviado, correo.Estado);
        Assert.Equal(1, correo.Intentos);
        Assert.Equal(_base.Reloj.AhoraUtc, correo.FechaEnvio);
    }

    // C-03 y C-04 (RF-NOT-10)
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Procesar_ConFallos_QuedaFallidoAlTercerIntento(bool fallaAlConectar)
    {
        // Preparar
        await EncolarAsync();
        _servidor.FallaAlConectar = fallaAlConectar;
        _servidor.FallaAlEnviar = !fallaAlConectar;

        // Actuar y verificar
        for (var intento = 1; intento <= ProcesadorColaCorreo.MaximoIntentos; intento++)
        {
            await ProcesarAsync();
            var correo = await LeerAsync();
            Assert.Equal(intento, correo.Intentos);
            Assert.Equal(intento < 3 ? EstadoCorreo.Pendiente : EstadoCorreo.Fallido, correo.Estado);
            Assert.Equal(ServidorFalso.Error, correo.UltimoError);
        }

        _servidor.FallaAlConectar = _servidor.FallaAlEnviar = false;
        await ProcesarAsync();
        Assert.Equal(0, _servidor.Enviados);
        Assert.Equal(3, (await LeerAsync()).Intentos);
    }

    // C-05 (RF-NOT-10)
    [Fact]
    public async Task Procesar_SinConfiguracion_NoSumaIntentos()
    {
        // Preparar
        await EncolarAsync();
        _variables.Remove(ConfiguracionSmtp.VariableHost);

        // Actuar
        var resultado = await ProcesarAsync();

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(0, (await LeerAsync()).Intentos);
    }

    // C-06 (RF-NOT-11)
    [Fact]
    public async Task Consultar_FiltraPorEstado()
    {
        // Preparar
        await EncolarAsync();
        await ProcesarAsync();
        await EncolarAsync();
        var consulta = new ServicioConsultaCorreos(_base.Contexto);

        // Actuar y verificar
        Assert.Equal(2, (await consulta.ListarAsync()).Count);
        Assert.Equal("Enviado", Assert.Single(await consulta.ListarAsync(EstadoCorreo.Enviado)).Estado);
        Assert.Empty(await consulta.ListarAsync(EstadoCorreo.Fallido));
    }

    private async Task EncolarAsync() =>
        await new ColaCorreo(_base.Contexto, _base.Reloj).EncolarAsync("ana@prueba.local", "Asunto", "Cuerpo");

    private Task<ResultadoProcesarCola> ProcesarAsync() =>
        new ProcesadorColaCorreo(_base.Contexto, _servidor, _base.Reloj, _variables.GetValueOrDefault).ProcesarPendientesAsync();

    private Task<CorreoEnCola> LeerAsync() => _base.Contexto.CorreosEnCola.AsNoTracking().SingleAsync();

    private sealed class ServidorFalso : IEnviadorCorreo, ISesionCorreo
    {
        public const string Error = "No se pudo conectar con el servidor.";

        public bool FallaAlConectar { get; set; }
        public bool FallaAlEnviar { get; set; }
        public int Enviados { get; private set; }

        public Task<ISesionCorreo> AbrirSesionAsync(ConfiguracionSmtp configuracion) =>
            FallaAlConectar ? throw new ErrorEnvioCorreo(Error, true) : Task.FromResult<ISesionCorreo>(this);

        public Task EnviarAsync(CorreoEnCola correo)
        {
            if (FallaAlEnviar)
                throw new ErrorEnvioCorreo(Error, false);
            Enviados++;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
