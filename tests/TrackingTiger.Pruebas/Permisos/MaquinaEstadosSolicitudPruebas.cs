using Tracking_Tiger.Core.Permisos;

namespace TrackingTiger.Pruebas.Permisos;

// Pruebas de la máquina de estados de Gestión de permisos (RD-04, RD-12): se ejercita
// MaquinaEstadosSolicitud directamente, sin base de datos, sin servidor y sin levantar la aplicación.
// Cada prueba crea sus propios datos, así que son independientes y pasan en cualquier orden.
public class MaquinaEstadosSolicitudPruebas
{
    private const int SolicitanteId = 1;
    private const int AdministradorId = 2;
    private static readonly DateTime Ahora = new(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);

    private static SolicitudPermiso NuevaSolicitudPendiente() =>
        SolicitudPermiso.Crear(SolicitanteId, "listar-usuarios", Ahora.AddHours(-1));

    // ---------- Transiciones válidas ----------

    // RF-GP-03
    [Fact]
    public void Aprobar_SolicitudPendiente_QuedaAprobadaConQuienAproboYCuando()
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();

        // Actuar
        var resultado = MaquinaEstadosSolicitud.IntentarTransicion(
            solicitud, EstadoSolicitud.Aprobada, AdministradorId, Ahora);

        // Verificar
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoSolicitud.Aprobada, solicitud.Estado);
        Assert.Equal(AdministradorId, solicitud.AprobadorId);
        Assert.Equal(Ahora, solicitud.FechaResolucion);
    }

    // RF-GP-04
    [Fact]
    public void Rechazar_SolicitudPendienteConMotivo_QuedaRechazadaConElMotivo()
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();

        // Actuar
        var resultado = MaquinaEstadosSolicitud.IntentarTransicion(
            solicitud, EstadoSolicitud.Rechazada, AdministradorId, Ahora, "  No lo necesita para su puesto.  ");

        // Verificar
        Assert.True(resultado.Exito);
        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.Equal("No lo necesita para su puesto.", solicitud.MotivoRechazo);
        Assert.Equal(AdministradorId, solicitud.RechazadaPorId);
        Assert.Equal(Ahora, solicitud.FechaResolucion);
    }

    // ---------- Transiciones prohibidas: se rechazan y el estado NO cambia (RF-GP-06) ----------

    // RF-GP-06: aprobar dos veces la misma solicitud.
    [Fact]
    public void Aprobar_SolicitudYaAplicada_SeRechazaYElEstadoNoCambia()
    {
        // Preparar: la solicitud ya se aprobó y el sistema la aplicó.
        var solicitud = NuevaSolicitudPendiente();
        MaquinaEstadosSolicitud.IntentarTransicion(solicitud, EstadoSolicitud.Aprobada, AdministradorId, Ahora);
        MaquinaEstadosSolicitud.IntentarTransicion(solicitud, EstadoSolicitud.Aplicada, null, Ahora);

        // Actuar: otro Administrador intenta aprobarla de nuevo, más tarde.
        var resultado = MaquinaEstadosSolicitud.IntentarTransicion(
            solicitud, EstadoSolicitud.Aprobada, 3, Ahora.AddHours(1));

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoSolicitud.Aplicada, solicitud.Estado);
        Assert.Equal(AdministradorId, solicitud.AprobadorId);
        Assert.Equal(Ahora, solicitud.FechaResolucion);
    }

    // RF-GP-06: pasar de Rechazada a Aplicada.
    [Fact]
    public void Aplicar_SolicitudRechazada_SeRechazaYElEstadoNoCambia()
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();
        MaquinaEstadosSolicitud.IntentarTransicion(
            solicitud, EstadoSolicitud.Rechazada, AdministradorId, Ahora, "No corresponde.");

        // Actuar
        var resultado = MaquinaEstadosSolicitud.IntentarTransicion(solicitud, EstadoSolicitud.Aplicada, null, Ahora);

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoSolicitud.Rechazada, solicitud.Estado);
        Assert.Null(solicitud.AprobadorId);
    }

    // RF-GP-06: de Pendiente no se salta directo a Aplicada (no está en la tabla).
    [Fact]
    public void Aplicar_SolicitudPendiente_SeRechazaYSigueEnPendiente()
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();

        // Actuar
        var resultado = MaquinaEstadosSolicitud.IntentarTransicion(solicitud, EstadoSolicitud.Aplicada, null, Ahora);

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
    }

    // RF-GP-04: sin motivo escrito, el rechazo no se completa.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rechazar_SinMotivoEscrito_SeRechazaYSigueEnPendiente(string? motivo)
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();

        // Actuar
        var resultado = MaquinaEstadosSolicitud.IntentarTransicion(
            solicitud, EstadoSolicitud.Rechazada, AdministradorId, Ahora, motivo);

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        Assert.Null(solicitud.MotivoRechazo);
        Assert.Null(solicitud.FechaResolucion);
    }

    // RF-GP-07: nadie aprueba su propia solicitud.
    [Fact]
    public void Aprobar_PropiaSolicitud_SeRechazaYSigueEnPendiente()
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();

        // Actuar: quien aprueba es el mismo solicitante.
        var resultado = MaquinaEstadosSolicitud.IntentarTransicion(
            solicitud, EstadoSolicitud.Aprobada, SolicitanteId, Ahora);

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        Assert.Null(solicitud.AprobadorId);
    }

    // RF-GP-07: nadie rechaza su propia solicitud.
    [Fact]
    public void Rechazar_PropiaSolicitud_SeRechazaYSigueEnPendiente()
    {
        // Preparar
        var solicitud = NuevaSolicitudPendiente();

        // Actuar
        var resultado = MaquinaEstadosSolicitud.IntentarTransicion(
            solicitud, EstadoSolicitud.Rechazada, SolicitanteId, Ahora, "Me arrepentí.");

        // Verificar
        Assert.False(resultado.Exito);
        Assert.Equal(EstadoSolicitud.Pendiente, solicitud.Estado);
        Assert.Null(solicitud.RechazadaPorId);
    }
}
