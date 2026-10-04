using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.ControlAcceso;

// Registro de usuarios con correo único (RF-CA-01). Toda la regla vive aquí, no en el controlador (RD-02).
// La cuenta queda con Activo en su valor por defecto; la activación llega en otro paso.
public sealed class ServicioRegistro
{
    // Debe coincidir con el límite declarado en ContextoDatos.
    private const int LargoMaximoNombre = 100;

    // Código extendido de SQLite para la violación de un índice único (SQLITE_CONSTRAINT_UNIQUE).
    private const int ErrorSqliteRestriccionUnica = 2067;

    private readonly ContextoDatos _contexto;
    private readonly IHasherContrasenas _hasher;
    private readonly IReloj _reloj;

    public ServicioRegistro(ContextoDatos contexto, IHasherContrasenas hasher, IReloj reloj)
    {
        _contexto = contexto;
        _hasher = hasher;
        _reloj = reloj;
    }

    public async Task<ResultadoRegistro> RegistrarAsync(string? nombre, string? correo, string? contrasena)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            return ResultadoRegistro.DatosInvalidos("El nombre es obligatorio.");

        nombre = nombre.Trim();
        if (nombre.Length > LargoMaximoNombre)
            return ResultadoRegistro.DatosInvalidos($"El nombre no puede tener más de {LargoMaximoNombre} caracteres.");

        var validacionCorreo = ValidadorCorreo.Validar(correo);
        if (!validacionCorreo.Valido)
            return ResultadoRegistro.DatosInvalidos(validacionCorreo.Mensaje!);

        var validacionContrasena = PoliticaContrasena.Validar(contrasena);
        if (!validacionContrasena.Valido)
            return ResultadoRegistro.DatosInvalidos(validacionContrasena.Mensaje!);

        var correoNormalizado = Usuario.NormalizarCorreo(correo!);

        if (await _contexto.Usuarios.AnyAsync(u => u.Correo == correoNormalizado))
            return ResultadoRegistro.CorreoDuplicado();

        var usuario = new Usuario
        {
            Nombre = nombre,
            Correo = correoNormalizado,
            HashContrasena = _hasher.Hashear(contrasena!),
            FechaCreacion = _reloj.AhoraUtc
        };

        _contexto.Usuarios.Add(usuario);
        try
        {
            await _contexto.SaveChangesAsync();
        }
        catch (DbUpdateException error) when (EsCorreoDuplicado(error))
        {
            // Carrera: otra petición registró el mismo correo entre la consulta y el guardado.
            // El índice único lo impidió; se responde igual que en la comprobación previa.
            _contexto.Entry(usuario).State = EntityState.Detached;
            return ResultadoRegistro.CorreoDuplicado();
        }

        return ResultadoRegistro.Registrado(new UsuarioRegistrado(usuario.Id, usuario.Nombre, usuario.Correo));
    }

    private static bool EsCorreoDuplicado(DbUpdateException error) =>
        error.InnerException is SqliteException sqlite
        && sqlite.SqliteExtendedErrorCode == ErrorSqliteRestriccionUnica;
}
