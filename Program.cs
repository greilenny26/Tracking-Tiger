using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.ControlAcceso;
using Tracking_Tiger.Core.Correo;
using Tracking_Tiger.Core.Persistencia;
using Tracking_Tiger.Core.Web;

var builder = WebApplication.CreateBuilder(args);

// El registro de peticiones de ASP.NET Core escribe la URL completa, con su query string, en nivel
// Information. GET /api/auth/activar?token=... dejaría el token en claro en la consola del servidor.
builder.Logging.AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(opciones =>
        opciones.InvalidModelStateResponseFactory = RespuestaSolicitudInvalida.Crear);

builder.Services.AddExceptionHandler<ManejadorErroresGlobal>();

builder.Services.AddSingleton<IReloj, RelojSistema>();
builder.Services.AddSingleton<IHasherContrasenas, HasherContrasenasPbkdf2>();

builder.Services.AddDbContext<ContextoDatos>(opciones =>
opciones.UseSqlite("Data Source=trackingtiger.db"));

builder.Services.AddScoped<EmisorActivacion>();
builder.Services.AddScoped<ServicioRegistro>();
builder.Services.AddScoped<ServicioActivacion>();
builder.Services.AddScoped<ServicioReenvioActivacion>();
builder.Services.AddScoped<ServicioSesion>();

builder.Services.AddScoped<IColaCorreo, ColaCorreo>();
builder.Services.AddSingleton<IEnviadorCorreo, EnviadorSmtp>();
builder.Services.AddScoped<ProcesadorColaCorreo>();

var app = builder.Build();

// Paso único de arranque (RD-09): aplica las migraciones pendientes antes de cualquier punto de entrada
// (servidor web, encolar-prueba, enviar-correos). Con un clon nuevo crea la base de datos desde cero;
// si ya está al día, no hace nada.
try
{
    using var alcanceMigracion = app.Services.CreateScope();
    alcanceMigracion.ServiceProvider.GetRequiredService<ContextoDatos>().Database.Migrate();
}
catch (Exception)
{
    // RD-08: no se muestran trazas ni consultas.
    Console.WriteLine("No se pudo preparar la base de datos. Verifica que el archivo no esté en uso y que haya permisos de escritura.");
    Environment.ExitCode = 1;
    return;
}

// Comando de desarrollo: dotnet run -- encolar-prueba <destinatario>
// Encola un correo de prueba y termina sin levantar el servidor web.
// Solo sirve para probar la cola y el enviador; la lógica vive en ColaCorreo.
if (args.Length > 0 && args[0] == "encolar-prueba")
{
    if (args.Length < 2)
    {
        Console.WriteLine("Uso: dotnet run -- encolar-prueba <destinatario>");
        Environment.ExitCode = 1;
        return;
    }

    using var alcance = app.Services.CreateScope();
    var cola = alcance.ServiceProvider.GetRequiredService<IColaCorreo>();
    var reloj = alcance.ServiceProvider.GetRequiredService<IReloj>();

    try
    {
        var resultado = await cola.EncolarAsync(
            args[1],
            "Correo de prueba de Tracking Tiger",
            $"Este es un correo de prueba de la cola. Encolado el {reloj.AhoraUtc:yyyy-MM-dd HH:mm:ss} UTC.");

        Console.WriteLine(resultado.Exito
            ? $"{resultado.Mensaje} Id: {resultado.CorreoId}"
            : $"Rechazado: {resultado.Mensaje}");
        Environment.ExitCode = resultado.Exito ? 0 : 1;
    }
    catch (Exception)
    {
        // RD-08: no se muestran trazas ni consultas.
        Console.WriteLine("No se pudo encolar el correo. Verifica que la base de datos esté creada y migrada.");
        Environment.ExitCode = 1;
    }

    return;
}

// Proceso independiente de envío (RF-NOT-09): dotnet run -- enviar-correos
// Envía los correos pendientes y termina sin levantar el servidor web. La lógica vive en ProcesadorColaCorreo.
if (args.Length > 0 && args[0] == "enviar-correos")
{
    using var alcance = app.Services.CreateScope();
    var procesador = alcance.ServiceProvider.GetRequiredService<ProcesadorColaCorreo>();

    try
    {
        var resultado = await procesador.ProcesarPendientesAsync();

        foreach (var aviso in resultado.Avisos)
            Console.WriteLine(aviso);
        Console.WriteLine(resultado.Mensaje);
        Environment.ExitCode = resultado.Exito ? 0 : 1;
    }
    catch (Exception)
    {
        // RD-08: no se muestran trazas, consultas ni datos del servidor.
        Console.WriteLine("No se pudo completar el envío de correos. Revisa la configuración SMTP y la conexión.");
        Environment.ExitCode = 1;
    }

    return;
}

// Manejo global de errores (RD-08): se aplica también en Development, así el cliente
// nunca ve la página de excepciones de desarrollo. La lógica está en ManejadorErroresGlobal.
app.UseExceptionHandler(_ => { });

app.MapControllers();

app.Run();
