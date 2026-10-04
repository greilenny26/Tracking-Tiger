using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Correo;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.ControlAcceso;

// Emite enlaces de activación (RF-CA-15, RF-CA-17): un solo lugar para el registro y el reenvío.
// En TokensActivacion solo queda el hash del token; el valor en claro solo viaja dentro del enlace
// del correo y nunca se registra en logs. Nada se guarda aquí: todo queda en la transacción
// del SaveChanges de quien llama, y el correo solo se encola (nunca se contacta a SMTP).
public sealed class EmisorActivacion
{
    private readonly ContextoDatos _contexto;
    private readonly IColaCorreo _cola;

    public EmisorActivacion(ContextoDatos contexto, IColaCorreo cola)
    {
        _contexto = contexto;
        _cola = cola;
    }

    // Genera un token nuevo y arma su enlace, sin tocar la base.
    // Devuelve null si falta APP_URL_BASE; error nombra la variable, nunca el token.
    public static EnlacePreparado? Preparar(out string? error)
    {
        var tokenPlano = GeneradorTokens.GenerarToken();
        var enlace = EnlaceActivacion.Construir(tokenPlano);

        error = enlace.Mensaje;
        return enlace.Exito
            ? new EnlacePreparado(GeneradorTokens.CalcularHash(tokenPlano), enlace.Enlace!)
            : null;
    }

    // Invalida los tokens anteriores sin usar del usuario y agrega al contexto el token nuevo
    // (vence en 24 h) y el correo con el enlace. No guarda: todo entra en el mismo SaveChanges.
    public async Task<ResultadoEncolar> EmitirAsync(Usuario usuario, EnlacePreparado enlace, DateTime ahoraUtc)
    {
        // RF-CA-17: al emitir un token nuevo, los enlaces anteriores dejan de funcionar.
        // Un usuario recién creado todavía no tiene tokens guardados: la consulta no encuentra nada.
        var anteriores = await _contexto.TokensActivacion
            .Where(t => t.UsuarioId == usuario.Id && !t.Usado && !t.Invalidado)
            .ToListAsync();
        foreach (var anterior in anteriores)
            anterior.Invalidar();

        _contexto.TokensActivacion.Add(TokenActivacion.Emitir(usuario, enlace.TokenHash, ahoraUtc));

        return _cola.Agregar(usuario.Correo, PlantillaCorreoActivacion.Asunto,
            PlantillaCorreoActivacion.Cuerpo(usuario.Nombre, enlace.Enlace));
    }
}

// Hash del token y enlace listo para el correo.
// Clase y no record: el ToString de un record imprimiría el enlace, que contiene el token.
public sealed class EnlacePreparado
{
    public string TokenHash { get; }
    public string Enlace { get; }

    public EnlacePreparado(string tokenHash, string enlace)
    {
        TokenHash = tokenHash;
        Enlace = enlace;
    }
}
