namespace Tracking_Tiger.Core.Correo;

// Datos de conexión al servidor SMTP, leídos de variables de entorno (RF-NOT-13, RD-10).
// No se leen al iniciar la aplicación: solo cuando el enviador los pide con Leer(),
// así la API funciona aunque SMTP no esté configurado.
public sealed class ConfiguracionSmtp
{
    public const string VariableHost = "SMTP_HOST";
    public const string VariablePuerto = "SMTP_PORT";
    public const string VariableUsuario = "SMTP_USUARIO";
    public const string VariableClave = "SMTP_CLAVE";
    public const string VariableRemitente = "SMTP_REMITENTE";

    private const int PuertoPredeterminado = 587;

    public string Host { get; }
    public int Puerto { get; }
    public string Usuario { get; }
    public string Clave { get; }
    public string Remitente { get; }

    private ConfiguracionSmtp(string host, int puerto, string usuario, string clave, string remitente)
    {
        Host = host;
        Puerto = puerto;
        Usuario = usuario;
        Clave = clave;
        Remitente = remitente;
    }

    // leerVariable permite sustituir el origen de las variables; por defecto, el entorno del proceso.
    public static ResultadoConfiguracionSmtp Leer(Func<string, string?>? leerVariable = null)
    {
        leerVariable ??= Environment.GetEnvironmentVariable;

        var host = leerVariable(VariableHost)?.Trim();
        var textoPuerto = leerVariable(VariablePuerto)?.Trim();
        var usuario = leerVariable(VariableUsuario)?.Trim();
        var clave = leerVariable(VariableClave);
        var remitente = leerVariable(VariableRemitente)?.Trim();

        var faltantes = new List<string>();
        if (string.IsNullOrWhiteSpace(host)) faltantes.Add(VariableHost);
        if (string.IsNullOrWhiteSpace(usuario)) faltantes.Add(VariableUsuario);
        if (string.IsNullOrWhiteSpace(clave)) faltantes.Add(VariableClave);

        if (faltantes.Count == 1)
            return ResultadoConfiguracionSmtp.Error(
                $"Falta configurar la variable de entorno {faltantes[0]} para enviar correos.");
        if (faltantes.Count > 1)
            return ResultadoConfiguracionSmtp.Error(
                $"Faltan por configurar las variables de entorno {string.Join(", ", faltantes)} para enviar correos.");

        var puerto = PuertoPredeterminado;
        if (!string.IsNullOrWhiteSpace(textoPuerto)
            && (!int.TryParse(textoPuerto, out puerto) || puerto < 1 || puerto > 65535))
            return ResultadoConfiguracionSmtp.Error(
                $"La variable de entorno {VariablePuerto} debe ser un número de puerto entre 1 y 65535.");

        // Sin remitente explícito se usa la cuenta de usuario (lo habitual en Gmail).
        if (string.IsNullOrWhiteSpace(remitente))
            remitente = usuario!;

        return ResultadoConfiguracionSmtp.Correcto(
            new ConfiguracionSmtp(host!, puerto, usuario!, clave!, remitente));
    }

    // Nunca muestra valores: así un log o un error no puede filtrar la clave por accidente.
    public override string ToString() => "ConfiguracionSmtp (valores ocultos)";
}

// Resultado de leer la configuración SMTP: la configuración o un mensaje que nombra la variable con problema.
public sealed class ResultadoConfiguracionSmtp
{
    public bool Exito => Configuracion is not null;
    public ConfiguracionSmtp? Configuracion { get; }
    public string? Mensaje { get; }

    private ResultadoConfiguracionSmtp(ConfiguracionSmtp? configuracion, string? mensaje)
    {
        Configuracion = configuracion;
        Mensaje = mensaje;
    }

    public static ResultadoConfiguracionSmtp Correcto(ConfiguracionSmtp configuracion) =>
        new(configuracion, null);

    public static ResultadoConfiguracionSmtp Error(string mensaje) =>
        new(null, mensaje);
}
