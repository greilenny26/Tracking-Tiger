using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Correo;

namespace Tracking_Tiger.Core.Persistencia;

// Contexto de base de datos del sistema.
public class ContextoDatos : DbContext
{
    public ContextoDatos(DbContextOptions<ContextoDatos> opciones)
        : base(opciones)
    {
    }

    public DbSet<CorreoEnCola> CorreosEnCola => Set<CorreoEnCola>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CorreoEnCola>(correo =>
        {
            correo.ToTable("CorreosEnCola");
            correo.HasKey(c => c.Id);
            correo.Property(c => c.Destinatario).IsRequired().HasMaxLength(320);
            correo.Property(c => c.Asunto).IsRequired().HasMaxLength(200);
            correo.Property(c => c.Cuerpo).IsRequired();
            correo.Property(c => c.Estado).IsRequired().HasConversion<string>().HasMaxLength(20);
            correo.Property(c => c.Intentos).IsRequired();
            correo.Property(c => c.FechaCreacion).IsRequired();
            correo.Property(c => c.UltimoError).HasMaxLength(1000);
            correo.HasIndex(c => c.Estado);
        });
    }
}
