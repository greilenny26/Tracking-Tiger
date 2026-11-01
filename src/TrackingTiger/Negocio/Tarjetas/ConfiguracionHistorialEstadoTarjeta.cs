using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tracking_Tiger.Core.ControlAcceso;

namespace Tracking_Tiger.Negocio.Tarjetas;

// Mapeo de HistorialEstadoTarjeta. El Core lo aplica con ApplyConfigurationsFromAssembly sin nombrarlo (RD-03).
public sealed class ConfiguracionHistorialEstadoTarjeta : IEntityTypeConfiguration<HistorialEstadoTarjeta>
{
    public void Configure(EntityTypeBuilder<HistorialEstadoTarjeta> historial)
    {
        historial.ToTable("HistorialEstadosTarjeta");
        historial.HasKey(h => h.Id);
        historial.Property(h => h.EstadoAnterior).IsRequired().HasConversion<string>().HasMaxLength(20);
        historial.Property(h => h.EstadoNuevo).IsRequired().HasConversion<string>().HasMaxLength(20);
        historial.Property(h => h.Nota).HasMaxLength(500);
        historial.Property(h => h.Fecha).IsRequired();
        historial.HasIndex(h => h.TarjetaId);
        // Restrict: el historial es auditoría; borrar una tarjeta o un usuario no debe borrarlo en silencio.
        historial.HasOne<Tarjeta>().WithMany().HasForeignKey(h => h.TarjetaId).OnDelete(DeleteBehavior.Restrict);
        historial.HasOne<Usuario>().WithMany().HasForeignKey(h => h.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
