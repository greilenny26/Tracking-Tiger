namespace Tracking_Tiger.Core.Correo;

// Modo de prueba (SMTP_HOST=consola): en lugar de enviar el correo, lo escribe en la salida del proceso, para
// probar el proyecto sin un servidor SMTP. Se ven los enlaces de activación y los códigos de recuperación,
// así que solo debe usarse en pruebas.
public sealed class SesionCorreoConsola : ISesionCorreo
{
    private readonly TextWriter _salida;

    public SesionCorreoConsola(TextWriter salida)
    {
        _salida = salida;
    }

    public async Task EnviarAsync(CorreoEnCola correo)
    {
        await _salida.WriteLineAsync("----- CORREO (modo consola) -----");
        await _salida.WriteLineAsync($"Para: {correo.Destinatario}");
        await _salida.WriteLineAsync($"Asunto: {correo.Asunto}");
        await _salida.WriteLineAsync();
        await _salida.WriteLineAsync(correo.Cuerpo);
        await _salida.WriteLineAsync("----- FIN DEL CORREO -----");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
