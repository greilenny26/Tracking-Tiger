using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tracking_Tiger.Negocio.Tarjetas;

// Mapeo de Transaccion. El Core lo aplica con ApplyConfigurationsFromAssembly sin nombrarlo (RD-03).
public sealed class ConfiguracionTransaccion : IEntityTypeConfiguration<Transaccion>
{
    public void Configure(EntityTypeBuilder<Transaccion> transaccion)
    {
        transaccion.ToTable("Transacciones");
        transaccion.HasKey(t => t.Id);
        transaccion.Property(t => t.Monto).IsRequired().HasPrecision(18, 2);
        transaccion.Property(t => t.Comercio).IsRequired().HasMaxLength(200);
        transaccion.Property(t => t.Fecha).IsRequired();
        transaccion.Property(t => t.Aprobada).IsRequired();
        transaccion.Property(t => t.MotivoDeclinacion).HasMaxLength(200);
        // La regla de ráfaga cuenta las declinaciones recientes de una tarjeta.
        transaccion.HasIndex(t => new { t.TarjetaId, t.Fecha });
        transaccion.HasOne<Tarjeta>().WithMany().HasForeignKey(t => t.TarjetaId).OnDelete(DeleteBehavior.Restrict);
    }
}
