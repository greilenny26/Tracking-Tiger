# Plan — Práctica 1: Control de acceso (Tracking Tiger)

Plan aprobado para implementar la pieza 1 del Core (RF-CA-01 a RF-CA-22), la cola mínima de correos (RF-NOT-08, 09, 12, 13), los requisitos de diseño que aplican (RD-05 a RD-11) y la estructura de la máquina de estados del negocio (RF-NEG-03, 04, 05, RD-04).

## Estado inicial del proyecto

| Elemento | Valor |
|---|---|
| Tipo de proyecto | Aplicación de consola (`Microsoft.NET.Sdk`, `OutputType` = `Exe`) |
| Framework | `net10.0` (ImplicitUsings y Nullable habilitados) |
| RootNamespace | `Tracking_Tiger` |
| Paquetes NuGet | Ninguno |
| Program.cs | Solo imprime "Hello, World!" |

## Decisiones aprobadas

- El SDK del proyecto cambia a `Microsoft.NET.Sdk.Web` (ASP.NET Core Web API).
- RootNamespace cambia a `TrackingTiger`.
- Se usan Minimal APIs (no controladores MVC).
- Un solo proyecto, con carpetas `Core/` y `Negocio/`.
- Vencimientos: enlace de activación 24 h, código de recuperación 15 min, sesión 8 h.
- Un Administrador no puede cambiar su propio rol.
- Se compila con `dotnet build` (no existe `skills.cmd`).
- Se agrega la variable de entorno `BD_RUTA` para la ruta del archivo SQLite (por defecto `tracking-tiger.db`).
- El esquema se crea con migraciones de EF Core (no con `EnsureCreated()`).
- `AlertaFraude` es la entidad central del negocio, no del Core: vive en `Negocio/`.

## 1. Estructura de carpetas y espacios de nombres

```
Tracking Tiger.csproj
Program.cs                        ← raíz de composición: modo API o comando "enviar-correos"
Core/                             namespace TrackingTiger.Core.*
  Comun/        IReloj, RelojSistema, Resultado, ErrorDeNegocio, ValidadorEntrada
  Datos/        ContextoDatos (DbContext), ConvencionesUtc, Migraciones/
  ControlAcceso/
    Entidades/     Usuario, Rol, TokenActivacion, SesionUsuario, CodigoRecuperacion
    Seguridad/     IHasherContrasenas, HasherContrasenasPbkdf2, GeneradorTokens, PoliticaContrasena
    Autorizacion/  Operacion, NivelAcceso, PoliticaDeAcceso, FiltroAutorizacion, UsuarioActual
    Servicios/     ServicioRegistro, ServicioSesion, ServicioContrasenas,
                   ServicioAdministracionUsuarios, SembradorAdministrador
    Endpoints/     EndpointsCuenta, EndpointsSesion, EndpointsUsuarios
  Correo/
    Entidades/     CorreoEnCola, EstadoCorreo
    ColaCorreos, PlantillasCorreo, ProcesadorColaCorreos,
    IEnviadorCorreo, EnviadorSmtp, ConfiguracionSmtp
  Web/          ManejadorErrores (RD-08), ExtensionesEndpoints
Negocio/                          namespace TrackingTiger.Negocio.*
  Alertas/      AlertaFraude, EstadoAlerta, MaquinaEstadosAlerta, ConfiguracionAlertaFraude
docs/maquina-de-estados.md, docs/pruebas.http
```

**Cómo se cumple RD-03:** `ContextoDatos` (Core) llama a `modelBuilder.ApplyConfigurationsFromAssembly(...)`. Así Negocio agrega su `IEntityTypeConfiguration<AlertaFraude>` sin que Core lo nombre, y Negocio accede con `contexto.Set<AlertaFraude>()`. Ningún archivo de `Core/` tiene `using TrackingTiger.Negocio`; si se elimina `Negocio/`, el proyecto sigue compilando y ejecutándose. Comprobación: `grep -r "Negocio" Core/` no devuelve resultados.

**Cómo se cumple RD-02:** los endpoints solo traducen HTTP ↔ llamada al servicio. Toda regla (bloqueo, autodesactivación, política de contraseña) vive en `Servicios/`.

## 2. Entidades y campos

Todas las fechas son `DateTime` en UTC.

- **Usuario:** Id, Nombre, Correo, CorreoNormalizado (índice único; sin espacios y en minúsculas), HashContrasena, Rol, Activo (lo controla el Administrador), CorreoVerificado, FechaCreacion, FechaActivacion?, IntentosFallidos, BloqueadoHasta?
  - "Nace inactivo" (RF-CA-15) significa `CorreoVerificado = false`. Para iniciar sesión se exige `CorreoVerificado && Activo`. Son dos indicadores distintos para que reactivar un usuario nunca se salte la activación por correo.
- **Rol** (enum): `Administrador`, `Estandar`. Columna obligatoria: todo usuario tiene exactamente un rol (RF-CA-04).
- **TokenActivacion:** Id, UsuarioId, TokenHash, FechaEmision, FechaVencimiento, FechaUso?, Invalidado (se marca al reenviar, RF-CA-17).
- **SesionUsuario:** Id, UsuarioId, TokenHash (índice único), FechaCreacion, FechaVencimiento, FechaRevocacion?, MotivoRevocacion? (CierreSesion, CambioContrasena, Desactivacion, RestablecimientoForzado).
- **CodigoRecuperacion:** Id, UsuarioId, CodigoHash, FechaEmision, FechaVencimiento, Usado, FechaUso?, IniciadoPorAdministrador (para RF-AUD-05 más adelante). El campo "código" de la especificación se guarda como hash.
- **CorreoEnCola:** Id, Destinatario, Asunto, Cuerpo, Estado (`Pendiente`, `Enviando`, `Enviado`, `Fallido`), Intentos, FechaCreacion, FechaEnvio?, UltimoError?, FechaReclamo?
- **AlertaFraude** (Negocio): Id, NumeroTarjetaEnmascarado (solo los últimos 4 dígitos), Monto, Comercio, FechaTransaccion, NivelRiesgo, Estado, FechaCreacion, FechaResolucion?, Observacion?
  - Estados: `Detectada → EnRevision → Confirmada | Descartada`.
  - Estados terminales: `Confirmada`, `Descartada`.
  - Transiciones prohibidas explícitas: `Descartada → Confirmada` y `Detectada → Confirmada` (primero debe revisarse).
  - Un único componente `MaquinaEstadosAlerta` con la tabla de transiciones y el método `IntentarTransicion(...)`.

## 3. Hash de contraseñas y tokens

### Contraseñas (RD-05, RF-CA-02)

- Sin paquetes adicionales: `Rfc2898DeriveBytes.Pbkdf2` (PBKDF2-HMAC-SHA256, 600 000 iteraciones, sal aleatoria de 16 bytes por usuario, resultado de 32 bytes).
- Se guarda en una sola cadena: `PBKDF2-SHA256$600000$<sal base64>$<hash base64>`, para poder subir las iteraciones en el futuro.
- La comparación usa `CryptographicOperations.FixedTimeEquals`.
- Misma contraseña ⇒ valor almacenado distinto (sal distinta).
- Si el correo no existe al iniciar sesión, igual se calcula un hash contra un valor ficticio para que el tiempo de respuesta sea el mismo.

**PoliticaContrasena (RF-CA-14):** mínimo 8 caracteres, al menos una letra y al menos un número. Se aplica en el registro, la recuperación, el restablecimiento forzado y el cambio de contraseña con sesión.

### Tokens de activación, sesión y código de recuperación

- Se generan con `RandomNumberGenerator`: los tokens son 32 bytes en Base64Url; el código de recuperación tiene 10 caracteres de un alfabeto sin caracteres ambiguos (~50 bits, fácil de escribir).
- Se guarda **solo el SHA-256 del valor** (alta entropía ⇒ no necesita sal; la búsqueda por hash es una consulta indexada directa).
- Un solo uso: se marca como usado con una actualización condicional (`WHERE Id = @id AND FechaUso IS NULL`), así dos peticiones simultáneas no pueden usarlo dos veces.
- Vencimientos: activación 24 h, recuperación 15 min, sesión 8 h (constantes en una sola clase).
- Invalidación de sesiones (RF-CA-12, 13, 18, 20): se revocan todas las sesiones abiertas del usuario en la misma transacción que el cambio de contraseña o la desactivación. Cada petición vuelve a comprobar que la sesión no esté revocada ni vencida **y** que `Usuario.Activo` sea verdadero.
- Restablecimiento forzado (RF-CA-13): se reemplaza `HashContrasena` por un marcador inutilizable (la contraseña anterior deja de servir de inmediato), se revocan las sesiones, se emite un `CodigoRecuperacion` y se encola el correo.
- Activación: `GET /cuenta/activar?token=…` muestra una página HTML con un botón "Activar" que hace el `POST`; solo el `POST` consume el token (evita que los escáneres de enlaces del correo lo gasten).

### Inicio de sesión (RF-CA-03, 15, 19)

- Correo inexistente o contraseña incorrecta ⇒ mismo mensaje: `"Correo o contraseña incorrectos."`
- Cuenta bloqueada (`BloqueadoHasta > ahora`) ⇒ se rechaza aun con la contraseña correcta: `"La cuenta está bloqueada temporalmente. Intenta de nuevo más tarde."`
- El 5.º fallo consecutivo fija `BloqueadoHasta = ahora + 15 min`; un inicio correcto pone `IntentosFallidos = 0`.
- El mensaje de "cuenta no activa" solo se muestra **después** de verificar que la contraseña es correcta, para no revelar qué correos existen.

## 4. Punto único donde se declara y valida el rol de cada operación

`Core/ControlAcceso/Autorizacion/PoliticaDeAcceso.cs` contiene una sola tabla legible:

```csharp
public enum NivelAcceso { Publico, Autenticado, Administrador }

public static class PoliticaDeAcceso
{
    public static readonly IReadOnlyDictionary<Operacion, NivelAcceso> Exigencias = new Dictionary<Operacion, NivelAcceso>
    {
        [Operacion.RegistrarUsuario]            = NivelAcceso.Publico,
        [Operacion.ActivarCuenta]               = NivelAcceso.Publico,
        [Operacion.ReenviarActivacion]          = NivelAcceso.Publico,
        [Operacion.IniciarSesion]               = NivelAcceso.Publico,
        [Operacion.SolicitarRecuperacion]       = NivelAcceso.Publico,
        [Operacion.RestablecerConCodigo]        = NivelAcceso.Publico,
        [Operacion.ConsultarUsuarioActual]      = NivelAcceso.Autenticado,
        [Operacion.CerrarSesion]                = NivelAcceso.Autenticado,
        [Operacion.CambiarContrasenaPropia]     = NivelAcceso.Autenticado,
        [Operacion.ListarUsuarios]              = NivelAcceso.Administrador,
        [Operacion.CambiarRol]                  = NivelAcceso.Administrador,
        [Operacion.DesactivarUsuario]           = NivelAcceso.Administrador,
        [Operacion.ReactivarUsuario]            = NivelAcceso.Administrador,
        [Operacion.ForzarRestablecimiento]      = NivelAcceso.Administrador,
    };
}
```

- Cada endpoint se etiqueta una sola vez con su operación, nunca con un rol: `.MapPut("/usuarios/{id}/rol", ...).ParaOperacion(Operacion.CambiarRol)`.
- `FiltroAutorizacion` (filtro de endpoint aplicado a todos los grupos de rutas) lee la operación, busca su exigencia en la tabla, lee `Authorization: Bearer <token>`, calcula su hash, carga la sesión y el usuario, y responde:
  - **401** `"Sesión no válida o vencida."`
  - **403** `"No tienes permiso para realizar esta operación."`
- Funciona igual para peticiones construidas a mano (RD-06, RF-CA-06).
- Falla cerrado al arrancar: la aplicación no inicia si alguna `Operacion` no está en la tabla o si algún endpoint no tiene `Operacion`.
- Las reglas que no son "qué rol" (un Administrador no se desactiva a sí mismo ni cambia su propio rol) viven en los servicios.

## 5. Cola de correos y comando enviador

- **Encolar:** los servicios llaman a `ColaCorreos.Encolar(destinatario, asunto, cuerpo)`, que solo agrega una fila `CorreoEnCola` (Estado = `Pendiente`) al mismo `DbContext`. Se guarda en el **mismo `SaveChanges`** que el usuario o el token: o existen ambos o ninguno. No hay SMTP dentro de la petición, así que el registro termina bien aunque el servidor de correo no responda (RF-NOT-08).
- **Comando enviador (RF-NOT-09):** el mismo ejecutable en otro modo:
  ```
  dotnet run -- enviar-correos
  ```
  `Program.cs` detecta el argumento, arma el mismo contenedor de dependencias sin servidor web, ejecuta `ProcesadorColaCorreos.ProcesarPendientesAsync()` una vez y termina con código 0 o 1.
- **Idempotencia (RF-NOT-12):** cada fila pendiente se reclama con una actualización condicional (`ExecuteUpdateAsync`):
  `UPDATE CorreoEnCola SET Estado = 'Enviando', FechaReclamo = @ahora WHERE Id = @id AND Estado = 'Pendiente'`.
  Solo si afectó 1 fila se envía.
  - Envío correcto ⇒ `Enviado` + `FechaEnvio`.
  - Error SMTP ⇒ vuelve a `Pendiente`, `Intentos++`, `UltimoError` (mensaje corto, sin secretos).
  - Ejecutarlo dos veces seguidas: la segunda no encuentra pendientes. Dos ejecuciones simultáneas no pueden reclamar la misma fila.
  - Si el proceso cae a mitad del envío, la fila queda en `Enviando` y no se reenvía automáticamente (como máximo una vez). Los reintentos y el estado `Fallido` llegan en la semana 11.
- **Cómo se ejecuta:** manualmente después de una operación (documentado en el README y en `docs/pruebas.http`); opcionalmente con el Programador de tareas de Windows. No hay servicio en segundo plano dentro de la API, así que el proceso es claramente independiente.

## 6. Variables de entorno

| Variable | Para qué sirve |
|---|---|
| `SMTP_HOST` | Servidor SMTP |
| `SMTP_PORT` | Puerto SMTP (587 STARTTLS o 465 SSL) |
| `SMTP_USUARIO` | Usuario de la cuenta SMTP |
| `SMTP_CLAVE` | Contraseña de aplicación de la cuenta SMTP |
| `SMTP_REMITENTE` | Dirección que aparece como remitente |
| `APP_URL_BASE` | URL base para construir el enlace de activación |
| `ADMIN_CORREO_INICIAL` | Correo del primer Administrador |
| `ADMIN_CLAVE_INICIAL` | Contraseña del primer Administrador |
| `BD_RUTA` | Ruta del archivo SQLite (por defecto `tracking-tiger.db`) |

- `ConfiguracionSmtp.DesdeEntorno()` lee las variables `SMTP_*` con `Environment.GetEnvironmentVariable`, valida que existan y que el puerto sea un número, y si falta alguna lanza un error controlado que nombra **la variable** (nunca su valor).
- Solo el modo `enviar-correos` exige las variables SMTP; la API arranca sin ellas.
- `EnviadorSmtp` (MailKit) usa `SecureSocketOptions.Auto`.
- Los valores se cargan desde `.env.cmd` en CMD (`call .env.cmd`) antes de `dotnet run`; el código nunca lee `.env.cmd`.
- `.env.example` lleva un comentario por variable (nombre y para qué sirve, nunca el valor).

## 7. Reloj UTC del sistema

```csharp
public interface IReloj { DateTime AhoraUtc { get; } }
public sealed class RelojSistema : IReloj { public DateTime AhoraUtc => DateTime.UtcNow; }
```

- Se registra como singleton; toda fecha de servicios y entidades sale de él (RD-11).
- Las pruebas usan un `RelojFijo` para simular vencimientos y el bloqueo de 15 minutos.
- `ContextoDatos.ConfigureConventions` agrega un convertidor de `DateTime` que fuerza `DateTimeKind.Utc` al leer (SQLite devuelve `Unspecified`).
- Se usa `DateTime` y no `DateTimeOffset`, porque EF Core con SQLite no puede comparar ni ordenar `DateTimeOffset` en el servidor.

## 8. Creación del primer Administrador

`SembradorAdministrador` se ejecuta al arrancar la API (después de aplicar las migraciones):

- Si **no** existe ningún usuario con `Rol.Administrador` **y** están definidas `ADMIN_CORREO_INICIAL` y `ADMIN_CLAVE_INICIAL`, crea ese usuario con la contraseña con hash, aplicando la política de contraseña, con `Activo = true` y `CorreoVerificado = true`.
- Es idempotente: no hace nada si ya existe un Administrador.
- Nunca registra la contraseña en los logs.
- Si faltan las variables, registra una advertencia y continúa.
- Los demás Administradores se crean con "cambiar rol" desde un Administrador existente.

## 9. Paquetes NuGet aprobados

| # | Paquete o cambio | Motivo |
|---|---|---|
| 0 | SDK `Microsoft.NET.Sdk` → `Microsoft.NET.Sdk.Web` (no es paquete, cambia el tipo de proyecto) | API web con ASP.NET Core |
| 1 | `Microsoft.EntityFrameworkCore.Sqlite` 10.x | Persistencia (RD-09) |
| 2 | `Microsoft.EntityFrameworkCore.Design` 10.x (`PrivateAssets=all`) | Migraciones |
| 3 | `MailKit` | Envío SMTP |
| 4 | Herramienta `dotnet-ef` (manifiesto local `.config/dotnet-tools.json`) | `dotnet ef migrations add` |

Pendiente de aprobación aparte: proyecto de pruebas con `xunit`, `xunit.runner.visualstudio` y `Microsoft.NET.Test.Sdk` (RD-12).

El hash, los tokens y el reloj no necesitan paquetes (vienen en .NET). No se usa JWT ni ASP.NET Identity.

## 10. Ramas y pull requests

1. `feat/base-api`: SDK web, SQLite, reloj UTC, manejador de errores, cola de correos y comando `enviar-correos`.
2. `feat/registro-activacion`: RF-CA-01, 02, 14, 15, 16, 17.
3. `feat/sesion`: RF-CA-03, 07, 18, 19, más `PoliticaDeAcceso` (RF-CA-05, 06).
4. `feat/recuperacion-contrasena`: RF-CA-09, 10, 11, 12, 22.
5. `feat/administracion-usuarios`: RF-CA-04, 08, 13, 20, 21, más `SembradorAdministrador`.
6. `feat/maquina-estados-alertas`: RF-NEG-03, 04, 05, RD-04, más `docs/maquina-de-estados.md`.

Cada rama: `dotnet build` sin errores, su caso en `docs/pruebas.http` y el README actualizado. Un commit = un ID de requisito.
