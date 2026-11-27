using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.Correo;

// Consulta de la cola (RF-NOT-11). No devuelve el cuerpo: lleva enlaces de activación y códigos de recuperación.
public sealed class ServicioConsultaCorreos
{
    private readonly ContextoDatos _contexto;

    public ServicioConsultaCorreos(ContextoDatos contexto)
    {
        _contexto = contexto;
    }

    public async Task<IReadOnlyList<CorreoListado>> ListarAsync(EstadoCorreo? estado = null)
    {
        var consulta = _contexto.CorreosEnCola.AsNoTracking();
        if (estado is not null)
            consulta = consulta.Where(c => c.Estado == estado.Value);

        var filas = await consulta.OrderBy(c => c.Id).ToListAsync();

        return filas
            .Select(c => new CorreoListado(c.Id, c.Destinatario, c.Asunto, c.Estado.ToString(), c.Intentos,
                c.FechaCreacion, c.FechaEnvio, c.UltimoError))
            .ToList();
    }
}

public sealed record CorreoListado(int Id, string Destinatario, string Asunto, string Estado, int Intentos,
    DateTime FechaCreacion, DateTime? FechaEnvio, string? UltimoError);
