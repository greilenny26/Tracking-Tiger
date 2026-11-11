using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.ControlAcceso;
using Tracking_Tiger.Core.Permisos;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Negocio.Tarjetas;

// Lógica del módulo Tarjetas (RD-02). Quién puede llamar a cada operación lo declara CatalogoOperaciones;
// los ids de usuario llegan siempre de la sesión del servidor. Los cambios de estado los decide
// MaquinaEstadosTarjeta (RD-04); este servicio solo carga, valida la entrada y guarda.
public sealed class ServicioTarjetas
{
    // Regla de ráfaga: estas declinaciones dentro de la ventana disparan el Bloqueo Suave.
    public const int DeclinacionesParaRafaga = 3;
    public const int MinutosVentanaRafaga = 60;

    public const decimal MontoMaximo = 1_000_000m;

    // El crédito de la tarjeta está en pesos; las compras en otra moneda se convierten a esta antes de comparar.
    public const string MonedaBase = "DOP";
    private static readonly string[] MonedasAdmitidas = [MonedaBase, "USD", "EUR"];

    private const int LargoMaximoTitular = 100;
    private const int LargoMaximoComercio = 200;
    private const int LargoMaximoNota = 500;

    private readonly ContextoDatos _contexto;
    private readonly IReloj _reloj;
    private readonly ServicioSolicitudesPermiso _permisos;
    private readonly IProveedorTasaCambio _tasasCambio;

    public ServicioTarjetas(ContextoDatos contexto, IReloj reloj, ServicioSolicitudesPermiso permisos, IProveedorTasaCambio tasasCambio)
    {
        _contexto = contexto;
        _reloj = reloj;
        _permisos = permisos;
        _tasasCambio = tasasCambio;
    }

    // Funcionalidad 1: emitir una tarjeta. Nace Activa y sin consumo.
    public async Task<ResultadoOperacionTarjeta> EmitirAsync(string? titular, string? ultimosCuatro, decimal? limiteCredito)
    {
        if (string.IsNullOrWhiteSpace(titular) || titular.Trim().Length > LargoMaximoTitular)
            return ResultadoOperacionTarjeta.DatoInvalido($"El titular es obligatorio y admite como máximo {LargoMaximoTitular} caracteres.");

        if (ultimosCuatro is null || ultimosCuatro.Length != 4 || !ultimosCuatro.All(char.IsAsciiDigit))
            return ResultadoOperacionTarjeta.DatoInvalido("ultimosCuatro debe tener exactamente 4 dígitos.");

        if (limiteCredito is null || limiteCredito <= 0 || limiteCredito > MontoMaximo)
            return ResultadoOperacionTarjeta.DatoInvalido($"limiteCredito es obligatorio, mayor que 0 y como máximo {MontoMaximo}.");

        var tarjeta = Tarjeta.Emitir(titular.Trim(), ultimosCuatro, limiteCredito.Value, _reloj.AhoraUtc);
        _contexto.Set<Tarjeta>().Add(tarjeta);
        await _contexto.SaveChangesAsync();
        return ResultadoOperacionTarjeta.Exito("Tarjeta emitida en estado Activa.", TarjetaDetalle.De(tarjeta));
    }

    // Funcionalidad 2: registrar una transacción. Se aprueba si la tarjeta está Activa y hay crédito disponible;
    // si no, se declina y queda registrada. Si una tarjeta Activa acumula DeclinacionesParaRafaga declinaciones en
    // MinutosVentanaRafaga minutos, el sistema la pasa a BloqueoSuave. En una tarjeta bloqueada las transacciones
    // se declinan, pero el estado no cambia por eso. Sin moneda, la compra es en DOP; en otra moneda se convierte a
    // DOP con la tasa del día, y si la tasa no se puede obtener la operación se rechaza sin registrar nada.
    public async Task<ResultadoOperacionTarjeta> RegistrarTransaccionAsync(int tarjetaId, decimal? monto, string? comercio, string? moneda = null)
    {
        if (monto is null || monto <= 0 || monto > MontoMaximo)
            return ResultadoOperacionTarjeta.DatoInvalido($"monto es obligatorio, mayor que 0 y como máximo {MontoMaximo}.");

        if (string.IsNullOrWhiteSpace(comercio) || comercio.Trim().Length > LargoMaximoComercio)
            return ResultadoOperacionTarjeta.DatoInvalido($"El comercio es obligatorio y admite como máximo {LargoMaximoComercio} caracteres.");

        var codigoMoneda = string.IsNullOrWhiteSpace(moneda) ? MonedaBase : moneda.Trim().ToUpperInvariant();
        if (!MonedasAdmitidas.Contains(codigoMoneda))
            return ResultadoOperacionTarjeta.DatoInvalido($"moneda debe ser una de estas: {string.Join(", ", MonedasAdmitidas)}.");

        var tarjeta = await BuscarAsync(tarjetaId);
        if (tarjeta is null)
            return ResultadoOperacionTarjeta.NoEncontrada();

        var montoEnPesos = monto.Value;
        ConversionMoneda? conversion = null;
        if (codigoMoneda != MonedaBase)
        {
            decimal tasa;
            try
            {
                tasa = await _tasasCambio.ObtenerTasaAsync(codigoMoneda, MonedaBase);
            }
            catch (ErrorTasaCambio error)
            {
                return ResultadoOperacionTarjeta.ServicioNoDisponible(error.Message);
            }

            montoEnPesos = Math.Round(monto.Value * tasa, 2, MidpointRounding.AwayFromZero);
            conversion = new ConversionMoneda(monto.Value, codigoMoneda, tasa);
        }

        var ahora = _reloj.AhoraUtc;
        var nombreComercio = comercio.Trim();
        Transaccion transaccion;
        string mensaje;

        if (tarjeta.Estado != EstadoTarjeta.Activa)
        {
            transaccion = Transaccion.Declinar(tarjeta.Id, montoEnPesos, nombreComercio,
                $"La tarjeta está en {tarjeta.Estado}.", ahora, conversion);
            mensaje = $"Transacción declinada: la tarjeta está en {tarjeta.Estado}.";
        }
        else if (montoEnPesos > tarjeta.Disponible)
        {
            transaccion = Transaccion.Declinar(tarjeta.Id, montoEnPesos, nombreComercio, "Crédito disponible insuficiente.", ahora, conversion);
            mensaje = "Transacción declinada: crédito disponible insuficiente.";

            var declinaciones = await ContarDeclinacionesRecientesAsync(tarjeta.Id, ahora) + 1;
            if (declinaciones >= DeclinacionesParaRafaga)
            {
                var bloqueo = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, EstadoTarjeta.BloqueoSuave, agenteId: null, ahora,
                    $"Ráfaga: {declinaciones} declinaciones en {MinutosVentanaRafaga} minutos.");
                if (bloqueo.Exito)
                {
                    _contexto.Set<HistorialEstadoTarjeta>().Add(bloqueo.Historial!);
                    mensaje += " La tarjeta pasó a BloqueoSuave por una ráfaga de declinaciones.";
                }
            }
        }
        else
        {
            tarjeta.RegistrarConsumo(montoEnPesos);
            transaccion = Transaccion.Aprobar(tarjeta.Id, montoEnPesos, nombreComercio, ahora, conversion);
            mensaje = "Transacción aprobada.";
        }

        _contexto.Set<Transaccion>().Add(transaccion);
        await _contexto.SaveChangesAsync();
        return ResultadoOperacionTarjeta.Exito(mensaje, TarjetaDetalle.De(tarjeta), TransaccionListado.De(transaccion));
    }

    // Funcionalidad 3: consultar una tarjeta.
    public async Task<ResultadoOperacionTarjeta> ConsultarAsync(int tarjetaId)
    {
        var tarjeta = await _contexto.Set<Tarjeta>().AsNoTracking().SingleOrDefaultAsync(t => t.Id == tarjetaId);
        return tarjeta is null
            ? ResultadoOperacionTarjeta.NoEncontrada()
            : ResultadoOperacionTarjeta.Exito($"La tarjeta está en {tarjeta.Estado}.", TarjetaDetalle.De(tarjeta));
    }

    // Funcionalidad 4: el agente registra que el cliente reconoce la actividad (BloqueoSuave → Activa).
    public Task<ResultadoOperacionTarjeta> ReconocerActividadAsync(int tarjetaId, int agenteId) =>
        CambiarEstadoAsync(tarjetaId, agenteId, EstadoTarjeta.BloqueoSuave, EstadoTarjeta.Activa, nota: null);

    // Funcionalidad 5: el agente registra que el cliente no reconoce la actividad (BloqueoSuave → BloqueoTotal).
    public Task<ResultadoOperacionTarjeta> NoReconocerActividadAsync(int tarjetaId, int agenteId, string? nota) =>
        CambiarEstadoAsync(tarjetaId, agenteId, EstadoTarjeta.BloqueoSuave, EstadoTarjeta.BloqueoTotal, nota);

    // Funcionalidad 6: el agente levanta un Bloqueo Total (BloqueoTotal → Activa). Exige un permiso
    // levantar-bloqueo-total aprobado por un Administrador y vigente en Gestión de permisos ("cuatro ojos").
    public async Task<ResultadoOperacionTarjeta> LevantarBloqueoTotalAsync(int tarjetaId, int agenteId, string? nota)
    {
        if (!await _permisos.TienePermisoVigenteAsync(agenteId, CatalogoOperaciones.LevantarBloqueoTotal))
            return ResultadoOperacionTarjeta.SinPermiso(
                $"Para levantar un Bloqueo Total necesitas el permiso {CatalogoOperaciones.LevantarBloqueoTotal} aprobado por un Administrador y vigente.");

        return await CambiarEstadoAsync(tarjetaId, agenteId, EstadoTarjeta.BloqueoTotal, EstadoTarjeta.Activa, nota);
    }

    // Funcionalidad 7: el agente cancela la tarjeta por pérdida o robo, desde cualquier estado no terminal.
    public Task<ResultadoOperacionTarjeta> CancelarAsync(int tarjetaId, int agenteId, string? nota) =>
        CambiarEstadoAsync(tarjetaId, agenteId, desdeEsperado: null, EstadoTarjeta.Cancelada, nota);

    // Funcionalidad 8: historial de transiciones de una tarjeta (lo consulta el Administrador). null si no existe.
    public async Task<IReadOnlyList<HistorialListado>?> ConsultarHistorialAsync(int tarjetaId)
    {
        if (!await _contexto.Set<Tarjeta>().AnyAsync(t => t.Id == tarjetaId))
            return null;

        var filas = await _contexto.Set<HistorialEstadoTarjeta>()
            .AsNoTracking()
            .Where(h => h.TarjetaId == tarjetaId)
            .OrderBy(h => h.Fecha)
            .ThenBy(h => h.Id)
            .ToListAsync();

        var ids = filas.Where(h => h.UsuarioId is not null).Select(h => h.UsuarioId!.Value).Distinct().ToList();
        var nombres = await _contexto.Usuarios.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Nombre);

        return filas
            .Select(h => new HistorialListado(h.Id, h.EstadoAnterior.ToString(), h.EstadoNuevo.ToString(), h.UsuarioId,
                h.UsuarioId is null ? "Sistema" : nombres.GetValueOrDefault(h.UsuarioId.Value, "Agente"), h.Nota, h.Fecha))
            .ToList();
    }

    // Carga la tarjeta, comprueba que quien actúa es un agente (Estándar) y le pide la transición a la máquina de
    // estados. Si la máquina la rechaza, no se guarda nada y la tarjeta no cambia.
    private async Task<ResultadoOperacionTarjeta> CambiarEstadoAsync(int tarjetaId, int agenteId,
        EstadoTarjeta? desdeEsperado, EstadoTarjeta hacia, string? nota)
    {
        if (nota is not null && nota.Trim().Length > LargoMaximoNota)
            return ResultadoOperacionTarjeta.DatoInvalido($"La nota admite como máximo {LargoMaximoNota} caracteres.");

        var tarjeta = await BuscarAsync(tarjetaId);
        if (tarjeta is null)
            return ResultadoOperacionTarjeta.NoEncontrada();

        // RF-NEG-06: CatalogoOperaciones ya lo exige en el endpoint; el servicio lo vuelve a comprobar.
        if (!await EsAgenteAsync(agenteId))
            return ResultadoOperacionTarjeta.NoPermitida("Las acciones sobre una tarjeta las ejecuta un agente (usuario Estándar).");

        var resultado = MaquinaEstadosTarjeta.IntentarTransicion(tarjeta, hacia, agenteId, _reloj.AhoraUtc, nota, desdeEsperado);
        if (!resultado.Exito)
            return ResultadoOperacionTarjeta.NoPermitida(resultado.Mensaje);

        _contexto.Set<HistorialEstadoTarjeta>().Add(resultado.Historial!);
        await _contexto.SaveChangesAsync();
        return ResultadoOperacionTarjeta.Exito(resultado.Mensaje, TarjetaDetalle.De(tarjeta));
    }

    private Task<Tarjeta?> BuscarAsync(int tarjetaId) =>
        _contexto.Set<Tarjeta>().SingleOrDefaultAsync(t => t.Id == tarjetaId);

    private Task<bool> EsAgenteAsync(int usuarioId) =>
        _contexto.Usuarios.AnyAsync(u => u.Id == usuarioId && u.Rol == Rol.Estandar);

    // Declinaciones de la tarjeta dentro de la ventana de ráfaga, sin contar las anteriores a su última
    // reactivación: si el cliente ya reconoció la actividad, esas declinaciones no la vuelven a bloquear.
    private async Task<int> ContarDeclinacionesRecientesAsync(int tarjetaId, DateTime ahoraUtc)
    {
        var desde = ahoraUtc.AddMinutes(-MinutosVentanaRafaga);
        var ultimaReactivacion = await _contexto.Set<HistorialEstadoTarjeta>()
            .Where(h => h.TarjetaId == tarjetaId && h.EstadoNuevo == EstadoTarjeta.Activa)
            .MaxAsync(h => (DateTime?)h.Fecha);
        if (ultimaReactivacion > desde)
            desde = ultimaReactivacion.Value;

        return await _contexto.Set<Transaccion>()
            .CountAsync(t => t.TarjetaId == tarjetaId && !t.Aprobada && t.Fecha > desde);
    }
}
