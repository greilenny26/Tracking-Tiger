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
    public const string CambiarContrasenaPropia = "cambiar-contrasena";
    public const string SolicitarRecuperacion = "recuperar";
    public const string Restablecer = "restablecer";
    public const string ListarUsuarios = "listar-usuarios";
    public const string CambiarRol = "cambiar-rol";
    public const string DesactivarUsuario = "desactivar-usuario";
    public const string ReactivarUsuario = "reactivar-usuario";
    public const string ForzarRestablecimiento = "forzar-restablecimiento";
    public const string CrearSolicitudPermiso = "crear-solicitud-permiso";
    public const string ConsultarSolicitudesPendientes = "solicitudes-pendientes";
    public const string ConsultarMisSolicitudes = "mis-solicitudes";
    public const string RechazarSolicitud = "rechazar-solicitud";

    // Gestión de permisos: operaciones que un usuario Estándar puede pedir con una solicitud. Nunca se
    // incluyen las que administran usuarios, roles o solicitudes: aprobar una de esas dejaría que alguien
    // se diera más privilegios a sí mismo (escalada de privilegios).
    public static readonly IReadOnlyList<string> OperacionesSolicitables = [ListarUsuarios];

    public static readonly IReadOnlyDictionary<string, ExigenciaAcceso> Exigencias = new Dictionary<string, ExigenciaAcceso>
    {
        [Registro]               = ExigenciaAcceso.Publica,
        [Activar]                = ExigenciaAcceso.Publica,
        [ReenviarActivacion]     = ExigenciaAcceso.Publica,
        [IniciarSesion]          = ExigenciaAcceso.Publica,
        [ConsultarUsuarioActual] = ExigenciaAcceso.Autenticada,
        [CerrarSesion]           = ExigenciaAcceso.Autenticada,
        [CambiarContrasenaPropia] = ExigenciaAcceso.Autenticada,
        [SolicitarRecuperacion]  = ExigenciaAcceso.Publica,
        [Restablecer]            = ExigenciaAcceso.Publica,
        // RF-GP-05: solicitable. La ejecuta el Administrador o quien tenga un permiso aprobado y vigente.
        [ListarUsuarios]         = ExigenciaAcceso.DeRolOPermiso(Rol.Administrador),
        [CambiarRol]             = ExigenciaAcceso.DeRol(Rol.Administrador),
        [DesactivarUsuario]      = ExigenciaAcceso.DeRol(Rol.Administrador),
        [ReactivarUsuario]       = ExigenciaAcceso.DeRol(Rol.Administrador),
        [ForzarRestablecimiento] = ExigenciaAcceso.DeRol(Rol.Administrador),
        // RF-GP-01: solo un usuario Estándar crea solicitudes (el Administrador ya tiene acceso).
        [CrearSolicitudPermiso]  = ExigenciaAcceso.DeRol(Rol.Estandar),
        // RF-GP-02: el Administrador ve las pendientes de todos; cada usuario, solo las suyas.
        [ConsultarSolicitudesPendientes] = ExigenciaAcceso.DeRol(Rol.Administrador),
        [ConsultarMisSolicitudes] = ExigenciaAcceso.Autenticada,
        // RF-GP-04: solo el Administrador resuelve; que no sea su propia solicitud lo decide la máquina (RF-GP-07).
        [RechazarSolicitud]      = ExigenciaAcceso.DeRol(Rol.Administrador),
    };
}

public enum TipoExigencia
{
    Publica,
    Autenticada,
    Rol,
    // RF-GP-05: el rol indicado, o cualquier usuario con un permiso otorgado y vigente para la operación.
    RolOPermiso
}

// Quién puede ejecutar una operación: cualquiera, cualquier usuario con sesión, o solo un rol concreto.
public sealed record ExigenciaAcceso(TipoExigencia Tipo, Rol? RolRequerido = null)
{
    public static readonly ExigenciaAcceso Publica = new(TipoExigencia.Publica);
    public static readonly ExigenciaAcceso Autenticada = new(TipoExigencia.Autenticada);
    public static ExigenciaAcceso DeRol(Rol rol) => new(TipoExigencia.Rol, rol);
    public static ExigenciaAcceso DeRolOPermiso(Rol rol) => new(TipoExigencia.RolOPermiso, rol);
}
