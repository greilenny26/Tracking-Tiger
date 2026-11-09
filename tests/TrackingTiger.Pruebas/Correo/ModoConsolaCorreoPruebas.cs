using Tracking_Tiger.Core.Correo;

namespace TrackingTiger.Pruebas.Correo;

// Modo consola del correo (RF-NOT-13): con SMTP_HOST=consola los correos se escriben en la salida del proceso.
public sealed class ModoConsolaCorreoPruebas
{
    // RF-NOT-13
    [Theory]
    [InlineData("consola", true)]
    [InlineData("CONSOLA", true)]
    [InlineData("smtp.gmail.com", false)]
    public void Leer_SegunElHost_ActivaElModoConsolaSoloConElValorConsola(string host, bool esperado)
    {
        // Preparar
        var variables = new Dictionary<string, string?>
        {
            [ConfiguracionSmtp.VariableHost] = host,
            [ConfiguracionSmtp.VariableUsuario] = "usuario@prueba.local",
            [ConfiguracionSmtp.VariableClave] = "clave"
        };

        // Actuar
        var resultado = ConfiguracionSmtp.Leer(variables.GetValueOrDefault);

        // Verificar
        Assert.True(resultado.Exito);
        Assert.Equal(esperado, resultado.Configuracion!.EsModoConsola);
    }

    // RF-NOT-13: el destinatario, el asunto y el cuerpo (con el enlace de activación) quedan visibles en la salida.
    [Fact]
    public async Task EnviarAsync_EnModoConsola_EscribeDestinatarioAsuntoYCuerpo()
    {
        // Preparar
        using var salida = new StringWriter();
        await using var sesion = new SesionCorreoConsola(salida);
        var correo = new CorreoEnCola
        {
            Destinatario = "ana@prueba.local",
            Asunto = "Activa tu cuenta",
            Cuerpo = "Abre http://localhost:5000/api/auth/activar?token=abc"
        };

        // Actuar
        await sesion.EnviarAsync(correo);

        // Verificar
        var texto = salida.ToString();
        Assert.Contains("ana@prueba.local", texto);
        Assert.Contains("Activa tu cuenta", texto);
        Assert.Contains("http://localhost:5000/api/auth/activar?token=abc", texto);
    }
}
