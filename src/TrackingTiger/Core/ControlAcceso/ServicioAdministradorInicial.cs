using Microsoft.EntityFrameworkCore;
using Tracking_Tiger.Core.Comun;
using Tracking_Tiger.Core.Persistencia;

namespace Tracking_Tiger.Core.ControlAcceso;

// Comando crear-admin (RF-CA-04): crea el primer Administrador a partir de variables de entorno (RD-10).
// Los mensajes nombran las variables, nunca sus valores; la contraseña jamás se imprime ni se registra.
// Si el correo ya existe no cambia nada: nunca promueve en silencio a un usuario existente.
public sealed class ServicioAdministradorInicial
{
    public const string VariableCorreo = "ADMIN_CORREO_INICIAL";
    public const string VariableClave = "ADMIN_CLAVE_INICIAL";
    private const string NombreAdministrador = "Administrador";

    private readonly ContextoDatos _contexto;
    private readonly IHasherContrasenas _hasher;
    private readonly IReloj _reloj;

    public ServicioAdministradorInicial(ContextoDatos contexto, IHasherContrasenas hasher, IReloj reloj)
    {
        _contexto = contexto;
        _hasher = hasher;
        _reloj = reloj;
    }

    // leerVariable permite sustituir el origen de las variables; por defecto, el entorno del proceso.
    public async Task<ResultadoAdministradorInicial> CrearAsync(Func<string, string?>? leerVariable = null)
    {
        leerVariable ??= Environment.GetEnvironmentVariable;
        var correo = leerVariable(VariableCorreo);
        var clave = leerVariable(VariableClave);

        var faltantes = new List<string>();
        if (string.IsNullOrWhiteSpace(correo)) faltantes.Add(VariableCorreo);
        if (string.IsNullOrEmpty(clave)) faltantes.Add(VariableClave);
        if (faltantes.Count == 1)
            return ResultadoAdministradorInicial.Error($"Falta configurar la variable de entorno {faltantes[0]}.");
        if (faltantes.Count > 1)
            return ResultadoAdministradorInicial.Error($"Faltan por configurar las variables de entorno {string.Join(", ", faltantes)}.");

        var validacionCorreo = ValidadorCorreo.Validar(correo);
        if (!validacionCorreo.Valido)
            return ResultadoAdministradorInicial.Error($"La variable de entorno {VariableCorreo} no es válida: {validacionCorreo.Mensaje}");

        var validacionClave = PoliticaContrasena.Validar(clave);
        if (!validacionClave.Valido)
            return ResultadoAdministradorInicial.Error($"La variable de entorno {VariableClave} no es válida: {validacionClave.Mensaje}");

        var correoNormalizado = Usuario.NormalizarCorreo(correo!);
        if (await _contexto.Usuarios.AnyAsync(u => u.Correo == correoNormalizado))
            return ResultadoAdministradorInicial.YaExiste();

        var administrador = Usuario.CrearAdministradorInicial(
            NombreAdministrador, correoNormalizado, _hasher.Hashear(clave!), _reloj.AhoraUtc);
        _contexto.Usuarios.Add(administrador);
        await _contexto.SaveChangesAsync();

        return ResultadoAdministradorInicial.Creado();
    }
}

public sealed record ResultadoAdministradorInicial(bool Exito, string Mensaje)
{
    public static ResultadoAdministradorInicial Creado() =>
        new(true, "Se creó el Administrador inicial, activo y listo para iniciar sesión.");

    // No es un error: ejecutar el comando otra vez no cambia nada.
    public static ResultadoAdministradorInicial YaExiste() =>
        new(true, "Ya existe un usuario con ese correo. No se cambió nada (no se promueve a un usuario existente).");

    public static ResultadoAdministradorInicial Error(string mensaje) => new(false, mensaje);
}
