using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.ControlAcceso;
using Tracking_Tiger.Core.Persistencia;

namespace TrackingTiger.Pruebas;

// Base SQLite en memoria con un reloj que las pruebas pueden mover. Cada prueba tiene la suya (RD-12).
public sealed class BaseEnMemoria : IDisposable
{
    private readonly SqliteConnection _conexion;

    public ContextoDatos Contexto { get; }
    public RelojAjustable Reloj { get; } = new(new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc));

    public BaseEnMemoria()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();
        var opciones = new DbContextOptionsBuilder<ContextoDatos>().UseSqlite(_conexion).Options;
        Contexto = new ContextoDatos(opciones);
        Contexto.Database.EnsureCreated();
    }

    public Task<int> CrearAdministradorAsync(string nombre) =>
        GuardarAsync(Usuario.CrearAdministradorInicial(nombre, $"{nombre}@prueba.local", "hash", Reloj.AhoraUtc));

    public Task<int> CrearEstandarAsync(string nombre)
    {
        var usuario = Usuario.CrearNuevo(nombre, $"{nombre}@prueba.local", "hash", Reloj.AhoraUtc);
        usuario.Activar();
        return GuardarAsync(usuario);
    }

    public async Task DesactivarAsync(int usuarioId)
    {
        (await Contexto.Usuarios.SingleAsync(u => u.Id == usuarioId)).Desactivar();
        await Contexto.SaveChangesAsync();
    }

    private async Task<int> GuardarAsync(Usuario usuario)
    {
        Contexto.Usuarios.Add(usuario);
        await Contexto.SaveChangesAsync();
        return usuario.Id;
    }

    public void Dispose()
    {
        Contexto.Dispose();
        _conexion.Dispose();
    }
}

public sealed class RelojAjustable(DateTime ahoraUtc) : IReloj
{
    public DateTime AhoraUtc { get; set; } = ahoraUtc;
}
