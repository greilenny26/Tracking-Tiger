using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Tracking_Tiger.Negocio.Alertas;

// Mapeo de AlertaFraude. El Core lo aplica con ApplyConfigurationsFromAssembly sin nombrarlo (RD-03).
public sealed class ConfiguracionAlertaFraude : IEntityTypeConfiguration<AlertaFraude>
{
    public void Configure(EntityTypeBuilder<AlertaFraude> alerta)
    {
        alerta.ToTable("AlertasFraude");
        alerta.HasKey(a => a.Id);
        alerta.Property(a => a.NumeroTarjetaEnmascarado).IsRequired().HasMaxLength(19);
        alerta.Property(a => a.Monto).IsRequired().HasPrecision(18, 2);
        alerta.Property(a => a.Comercio).IsRequired().HasMaxLength(200);
        alerta.Property(a => a.FechaTransaccion).IsRequired();
        alerta.Property(a => a.NivelRiesgo).IsRequired();
        // RF-NEG-03: el estado se guarda como texto, legible en DB Browser.
        alerta.Property(a => a.Estado).IsRequired().HasConversion<string>().HasMaxLength(20);
        alerta.Property(a => a.FechaCreacion).IsRequired();
        alerta.Property(a => a.Observacion).HasMaxLength(1000);
        alerta.HasIndex(a => a.Estado);
    }
}
