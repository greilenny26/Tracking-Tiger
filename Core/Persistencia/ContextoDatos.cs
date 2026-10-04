using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.ControlAcceso;
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
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TokenActivacion> TokensActivacion => Set<TokenActivacion>();

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

        modelBuilder.Entity<Usuario>(usuario =>
        {
            usuario.ToTable("Usuarios");
            usuario.HasKey(u => u.Id);
            usuario.Property(u => u.Nombre).IsRequired().HasMaxLength(100);
            usuario.Property(u => u.Correo).IsRequired().HasMaxLength(320);
            usuario.Property(u => u.HashContrasena).IsRequired();
            usuario.Property(u => u.Activo).IsRequired();
            usuario.Property(u => u.FechaCreacion).IsRequired();
            // RF-CA-01: la base rechaza un segundo usuario con el mismo correo.
            usuario.HasIndex(u => u.Correo).IsUnique();
        });

        modelBuilder.Entity<TokenActivacion>(token =>
        {
            token.ToTable("TokensActivacion");
            token.HasKey(t => t.Id);
            token.Property(t => t.TokenHash).IsRequired().HasMaxLength(64);
            token.Property(t => t.FechaEmision).IsRequired();
            token.Property(t => t.FechaVencimiento).IsRequired();
            token.Property(t => t.Usado).IsRequired();
            // La activación busca el token por su hash.
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasOne(t => t.Usuario)
                .WithMany()
                .HasForeignKey(t => t.UsuarioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
