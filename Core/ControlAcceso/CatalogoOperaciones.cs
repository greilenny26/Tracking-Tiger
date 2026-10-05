namespace Tracking_Tiger.Core.ControlAcceso;

// RF-CA-05: ÚNICO lugar donde se declara quién puede ejecutar cada operación del sistema.
// Lo aplica FiltroAutorizacionOperaciones en el servidor (RD-06). Una operación que no está aquí
// se rechaza (denegar por defecto). Para una operación nueva: agregar su constante y su línea
// en Exigencias, y marcar el endpoint con [Operacion(CatalogoOperaciones.X)].
public static class CatalogoOperaciones
{
    public const string Registro = "registro";
    public const string Activar = "activar";
    public const string ReenviarActivacion = "reenviar-activacion";
    public const string IniciarSesion = "login";
    public const string ConsultarUsuarioActual = "yo";
    public const string CerrarSesion = "logout";
    public const string SolicitarRecuperacion = "recuperar";
    public const string ListarUsuarios = "listar-usuarios";
    public const string CambiarRol = "cambiar-rol";
    public const string DesactivarUsuario = "desactivar-usuario";
    public const string ReactivarUsuario = "reactivar-usuario";

    public static readonly IReadOnlyDictionary<string, ExigenciaAcceso> Exigencias = new Dictionary<string, ExigenciaAcceso>
    {
        [Registro]               = ExigenciaAcceso.Publica,
        [Activar]                = ExigenciaAcceso.Publica,
        [ReenviarActivacion]     = ExigenciaAcceso.Publica,
        [IniciarSesion]          = ExigenciaAcceso.Publica,
        [ConsultarUsuarioActual] = ExigenciaAcceso.Autenticada,
        [CerrarSesion]           = ExigenciaAcceso.Autenticada,
        [SolicitarRecuperacion]  = ExigenciaAcceso.Publica,
        [ListarUsuarios]         = ExigenciaAcceso.DeRol(Rol.Administrador),
        [CambiarRol]             = ExigenciaAcceso.DeRol(Rol.Administrador),
        [DesactivarUsuario]      = ExigenciaAcceso.DeRol(Rol.Administrador),
        [ReactivarUsuario]       = ExigenciaAcceso.DeRol(Rol.Administrador),
    };
}

public enum TipoExigencia
{
    Publica,
    Autenticada,
    Rol
}

// Quién puede ejecutar una operación: cualquiera, cualquier usuario con sesión, o solo un rol concreto.
public sealed record ExigenciaAcceso(TipoExigencia Tipo, Rol? RolRequerido = null)
{
    public static readonly ExigenciaAcceso Publica = new(TipoExigencia.Publica);
    public static readonly ExigenciaAcceso Autenticada = new(TipoExigencia.Autenticada);
    public static ExigenciaAcceso DeRol(Rol rol) => new(TipoExigencia.Rol, rol);
}
