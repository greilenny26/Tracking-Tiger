using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tracking_Tiger.Negocio.Tarjetas;

// Mapeo de Tarjeta. El Core lo aplica con ApplyConfigurationsFromAssembly sin nombrarlo (RD-03).
public sealed class ConfiguracionTarjeta : IEntityTypeConfiguration<Tarjeta>
{
    public void Configure(EntityTypeBuilder<Tarjeta> tarjeta)
    {
        tarjeta.ToTable("Tarjetas");
        tarjeta.HasKey(t => t.Id);
        tarjeta.Property(t => t.Titular).IsRequired().HasMaxLength(100);
        tarjeta.Property(t => t.UltimosCuatro).IsRequired().HasMaxLength(4);
        tarjeta.Property(t => t.LimiteCredito).IsRequired().HasPrecision(18, 2);
        tarjeta.Property(t => t.Consumido).IsRequired().HasPrecision(18, 2);
        // RF-NEG-03: el estado se guarda como texto, legible en DB Browser.
        tarjeta.Property(t => t.Estado).IsRequired().HasConversion<string>().HasMaxLength(20);
        tarjeta.Property(t => t.FechaEmision).IsRequired();
        tarjeta.Ignore(t => t.Disponible);
        tarjeta.Ignore(t => t.NumeroEnmascarado);
    }
}
