using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.ControlAcceso;
using Tracking_Tiger.Core.Correo;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.Notificaciones;

// Lógica de Notificaciones (RD-02). Los demás módulos le dicen a quién avisar y qué decir;
// este servicio entrega por los dos canales (RF-NOT-06). Los ids de usuario de las consultas
// llegan siempre de la sesión del servidor.
public sealed class ServicioNotificaciones
{
    private readonly ContextoDatos _contexto;
    private readonly IReloj _reloj;
    private readonly IColaCorreo _cola;

    public ServicioNotificaciones(ContextoDatos contexto, IReloj reloj, IColaCorreo cola)
    {
        _contexto = contexto;
        _reloj = reloj;
        _cola = cola;
    }

    // Agrega al contexto, SIN guardar, una notificación por destinatario y, si tiene el canal de correo
    // activado (RF-NOT-07), su correo en la cola. Quien llama guarda todo junto con su operación:
    // o quedan la operación y sus avisos, o no queda nada. Nunca contacta al servidor de correo (RF-NOT-08).
    public async Task AgregarAsync(IEnumerable<int> destinatariosIds, string tipo, string asunto, string mensaje)
    {
        var ids = destinatariosIds.Distinct().ToList();
        if (ids.Count == 0)
            return;

        if (mensaje.Length > Notificacion.LargoMaximoMensaje)
            mensaje = mensaje[..Notificacion.LargoMaximoMensaje];

        var destinatarios = await _contexto.Usuarios
            .Where(u => ids.Contains(u.Id))
            .OrderBy(u => u.Id)
            .ToListAsync();

        var ahora = _reloj.AhoraUtc;
        foreach (var usuario in destinatarios)
        {
            _contexto.Notificaciones.Add(Notificacion.Crear(usuario.Id, tipo, mensaje, ahora));

            if (usuario.NotificacionesPorCorreo)
                _cola.Agregar(usuario.Correo, asunto, mensaje);
        }
    }

    // RF-NOT-01: avisa a todos los Administradores activos y a nadie más.
    public async Task AgregarParaAdministradoresAsync(string tipo, string asunto, string mensaje)
    {
        var administradores = await _contexto.Usuarios
            .Where(u => u.Rol == Rol.Administrador && u.Activo)
            .Select(u => u.Id)
            .ToListAsync();

        await AgregarAsync(administradores, tipo, asunto, mensaje);
    }

    // RF-NOT-04: solo las del usuario indicado; leida filtra leídas o no leídas. La más reciente primero.
    public async Task<IReadOnlyList<NotificacionListado>> ListarAsync(int usuarioId, bool? leida = null)
    {
        var consulta = _contexto.Notificaciones.AsNoTracking().Where(n => n.DestinatarioId == usuarioId);
        if (leida is not null)
            consulta = consulta.Where(n => n.Leida == leida.Value);

        return await consulta
            .OrderByDescending(n => n.Fecha)
            .ThenByDescending(n => n.Id)
            .Select(n => new NotificacionListado(n.Id, n.Tipo, n.Mensaje, n.Leida, n.Fecha))
            .ToListAsync();
    }

    // RF-NOT-05: solo el destinatario puede marcarla, tenga el rol que tenga.
    public async Task<ResultadoMarcarLeida> MarcarLeidaAsync(int notificacionId, int usuarioId)
    {
        var notificacion = await _contexto.Notificaciones.SingleOrDefaultAsync(n => n.Id == notificacionId);
        if (notificacion is null)
            return ResultadoMarcarLeida.NoEncontrada;
        if (notificacion.DestinatarioId != usuarioId)
            return ResultadoMarcarLeida.Ajena;

        notificacion.MarcarLeida();
        await _contexto.SaveChangesAsync();
        return ResultadoMarcarLeida.Marcada;
    }

    // RF-NOT-07: ¿el usuario recibe además el correo?
    public Task<bool> ConsultarCorreoActivadoAsync(int usuarioId) =>
        _contexto.Usuarios.Where(u => u.Id == usuarioId).Select(u => u.NotificacionesPorCorreo).SingleAsync();

    // RF-NOT-07: activa o desactiva el canal de correo. El canal interno no se puede desactivar.
    public async Task CambiarCorreoAsync(int usuarioId, bool activado)
    {
        var usuario = await _contexto.Usuarios.SingleAsync(u => u.Id == usuarioId);
        usuario.CambiarNotificacionesPorCorreo(activado);
        await _contexto.SaveChangesAsync();
    }
}

public enum ResultadoMarcarLeida
{
    Marcada,
    NoEncontrada,
    Ajena
}

public sealed record NotificacionListado(int Id, string Tipo, string Mensaje, bool Leida, DateTime Fecha);
