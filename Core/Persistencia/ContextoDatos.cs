using Microsoft.EntityFrameworkCore;

namespace Tracking_Tiger.Core.Persistencia;

// Contexto de base de datos del sistema. Todavía no tiene entidades.
public class ContextoDatos : DbContext
{
    public ContextoDatos(DbContextOptions<ContextoDatos> opciones)
        : base(opciones)
    {
    }
}
