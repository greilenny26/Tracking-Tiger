# Bitácora — Práctica 1

## Sesión 1: Configuración del entorno de desarrollo y variables de entorno

- **Qué le pedí:** "B8. Variables de entorno en Windows... B11. Commit de preparación" y ayuda para verificar la configuración de `.env.cmd` e ignorado en Git.
- **Qué devolvió:** Explicación del funcionamiento de `call .env.cmd`, corrección del uso de terminal (cambio de PowerShell a CMD), indicación para remover espacios en la clave de aplicación de Gmail, y plantilla del Pull Request para GitHub.
- **Dónde se equivocó:** Sin errores; verifiqué:
  - Que el archivo `.env.cmd` contuviera la sintaxis correcta para CMD (`set CLAVE=VALOR`).
  - Que la contraseña de aplicación de Google no incluyera espacios (`xxxxxxxxxxxxxxxx`).
  - Que la terminal usada para ejecutar `call .env.cmd` fuera Command Prompt (CMD) y no PowerShell.
  - Que `git check-ignore -v .env.cmd` confirmara que la regla `.env.*` en el `.gitignore` bloquea correctamente el archivo sensible.
- **Cómo lo detecté:** Revisé los comandos en la terminal de VS Code y ejecuté `git status` y `git check-ignore` para comprobar que Git no rastreara credenciales.
- **Cómo lo corregí:** Cambié el perfil de la terminal de VS Code de PowerShell a CMD, removí los espacios de la clave de Gmail y corregí el error de tipeo en el comando `git add .gitignore`.
- **Commit(s):** 
  - `491a560` Agrega reglas de commits y requisitos al repositorio (Bitácora)

## Sesión 2: Conversión a API Web y persistencia con SQLite

- **Qué le pedí:** Convertir el proyecto de consola en una API Web de ASP.NET Core con controladores y configurar la base de datos SQLite con EF Core mediante `ContextoDatos`.
- **Qué devolvió:**
  - Cambio del SDK del proyecto a `Microsoft.NET.Sdk.Web`, `Properties/launchSettings.json` (perfiles `http` y `https`, entorno `Development`) y `Program.cs` con el modelo de alojamiento mínimo y `AddControllers()` / `MapControllers()`.
  - Paquetes permitidos `Microsoft.EntityFrameworkCore.Sqlite` y `Microsoft.EntityFrameworkCore.Design`.
  - `Core/Persistencia/ContextoDatos.cs` registrado en `Program.cs` con `UseSqlite("Data Source=trackingtiger.db")`.
- **Dónde se equivocó:** Sin errores. La conversión y configuración inicial se realizaron de forma limpia y directa.
- **Cómo lo detecté:**  No hubo errores que detectar; verifiqué la compilación exitosa y el funcionamiento de la persistencia.gitignore`.
- **Cómo lo corregí:**  No se requirieron correcciones.
- **Commit(s):**
  - `77584b0` Configura el modelo de alojamiento minimo y controladores en Program.cs
  - `c909290` Actualiza el README con las instrucciones de la Web API
  - `fb3486f` Convierte el proyecto en API Web
  - `f9a5908` Agrega persistencia con SQLite

## Sesión 3: Reloj único en UTC, entidad CorreoEnCola y su migración

- **Qué le pedí:** Preparar la base de la cola de correos (RF-NOT-08): un reloj único del sistema y la entidad `CorreoEnCola` con su migración.
- **Qué devolvió:**
  - `IReloj` y `RelojSistema` en `Core/Comun/`: único punto que lee la hora, siempre en UTC (RD-11), registrado como singleton.
  - `CorreoEnCola` y `EstadoCorreo` (Pendiente, Enviado, Fallido) en `Core/Correo/`, con destinatario, asunto, cuerpo, estado, intentos, fecha de creación, fecha de envío y último error.
  - Mapeo en `ContextoDatos` (tabla `CorreosEnCola`, estado guardado como texto, índice por estado, largos máximos) y la migración `CrearCorreoEnCola`.
- **Dónde se equivocó:** No hubo errores que detectar; comprobé la correcta aplicación de la migración en la base de datos
- **Cómo lo detecté:** Sin errores. La estructura del reloj, la entidad y la migración se generaron correctamente a la primera.
- **Cómo lo corregí:** No se requirieron correcciones.
- **Commit(s):**
  - `2340bf9` Agrega un reloj unico del sistema en UTC
  - `0f872b4` Agrega la entidad CorreoEnCola con su migracion
  - `c7a1252` Agrega la entidad CorreoEnCola

## Sesión 4: Servicio de cola de correos y comando `encolar-prueba`

- **Qué le pedí:** "Agrega el servicio de cola de correos con una operación EncolarAsync(destinatario, asunto, cuerpo)... NO debe contactar ningún servidor SMTP... Agrega además un comando de desarrollo en Program.cs: `dotnet run -- encolar-prueba <destinatario>`".
- **Qué devolvió:**
  - `IColaCorreo` y `ColaCorreo` en `Core/Correo/`: valida que los tres valores no estén vacíos y no superen los largos de la tabla, crea la fila con estado `Pendiente`, `Intentos = 0` y `FechaCreacion` tomada de `IReloj`, y la guarda. No tiene código SMTP.
  - `ResultadoEncolar`: una entrada inválida se rechaza con un mensaje comprensible, sin excepción (RD-07, RD-08).
  - Comando `encolar-prueba` en `Program.cs`: solo lee los argumentos, llama al servicio e imprime el resultado; termina sin iniciar el servidor web.
  - Caso de prueba documentado en `docs/pruebas.http`.
- **Dónde se equivocó:** Al probar, el comando de compilación (`skills.cmd build`) falló porque lo ejecutó desde una terminal que no encontraba el script, y luego corrió `dotnet run --no-build` con el binario anterior, que todavía no tenía el comando: se levantó el servidor web en `http://localhost:5000`.
- **Cómo lo detecté:** La salida mostró "Now listening on: http://localhost:5000" en lugar del mensaje del comando.
- **Cómo lo corregí:** Detuvo el proceso, comprobó que no quedara el servidor corriendo, compiló con `skills.cmd build` desde PowerShell (0 advertencias, 0 errores) y repitió las pruebas con un límite de tiempo.
- **Verificación:**
  - `dotnet run -- encolar-prueba prueba@example.com` → "El correo quedó en la cola en estado pendiente. Id: 1", sin iniciar el servidor.
  - `dotnet run -- encolar-prueba "   "` → "Rechazado: El destinatario es obligatorio.", sin traza.
  - `dotnet run -- encolar-prueba` → muestra el modo de uso.
  - Consulta directa a `trackingtiger.db`: la tabla `CorreosEnCola` tiene las filas encoladas con `Estado = Pendiente`, `Intentos = 0`, `FechaCreacion` en UTC y `FechaEnvio` / `UltimoError` vacíos.
- **Commit(s):**
  - `74d7fef` Agrega el servicio de cola de correos

## Sesión 5: Registro de usuarios y restricción de correo único (RF-CA-01)
- **Qué le pedí:**: Implementar la entidad Usuario, su migración correspondiente y la lógica de registro de usuarios validando la unicidad del correo.
- **Qué devolvió:**
Creación de la entidad Usuario y registro del modelo con índice único IX_Usuarios_Correo en ContextoDatos.
Generación de la migración CrearUsuario (20261004104928_CrearUsuario.cs).
Lógica de registro para procesar y validar el correo normalizado, asegurando el almacenamiento seguro de la contraseña y la fecha de creación en UTC.
Adición de los casos de prueba iniciales de registro en docs/pruebas.http.

- **Dónde se equivocó:** Hubo un pequeño desfase de líneas al intentar aplicar un parche automatizado (patch does not apply) sobre el archivo de pruebas HTTP debido a modificaciones locales previas.

- **Cómo lo detecté:** El comando git apply de Git devolvió un error de conflicto de parche en docs/pruebas.http.

- **Cómo lo corregí:**  Se descartó el parche automatizado y se manejó el flujo de manera limpia realizando un git add directo de los archivos modificados y nuevos del código fuente.

- **Commit(s):**


 - `9ea0628` Agrega la activacion de cuentas por enlace

 - `b891c3d`  Encolar el correo de activacion al registrar

 - `0c59de6`  Permite encolar correos en la transaccion de la operacion

 - `0936998`  Genera el token de activacion al registrar 

## Sesión 6: Reenvío del enlace de activación y unificación de respuestas (RF-CA-16 y RF-CA-17)
- **Qué le pedí:**: Implementar la funcionalidad de reenvío del enlace de activación (RF-CA-17), asegurar la invalidación de tokens previos y unificar la respuesta del endpoint, además de retirar tokens reales de la documentación de pruebas (RF-CA-16).

- **Qué devolvió:**
Nuevos componentes en Core/ControlAcceso/: EmisorActivacion.cs, ResultadoReenvio.cs, ServicioReenvioActivacion.cs y SolicitudReenvioActivacion.cs.
Actualización de servicios y controladores (ControladorAutenticacion.cs, ServicioActivacion.cs, ServicioRegistro.cs, TokenActivacion.cs, ContextoDatos.cs y Program.cs).
Migración InvalidarTokensActivacion (20261004195656_InvalidarTokensActivacion.cs).
Actualización del archivo docs/pruebas.http para eliminar tokens reales expuestos.

- **Dónde se equivocó:** Al intentar aplicar parches automáticos cruzados, Git requirió organización manual del staging area para separar el commit de limpieza de documentación del commit funcional.

- **Cómo lo detecté:** Mediante los mensajes de estado de git status que mostraban archivos modificados y rastreos pendientes.
- **Cómo lo corregí:** Se ejecutaron los comandos git add y git commit de forma secuencial y limpia para mantener la higiene de commits exigida por el proyecto.

- **Commit(s):**
 - `e97e501`  Agrega el reenvio del enlace de activacion

- `a81c37e` Quita el token real de las pruebas de activacion

- `985a20f` Rechaza el enlace de activacion vencido

- `e7a2836` Rechaza el enlace de activacion vencido

## Sesión 7: Migraciones automáticas al iniciar (RD-09)
- **Qué le pedí:** Que la aplicación aplique todas las migraciones pendientes de EF Core al iniciar, en un solo paso compartido por el servidor web y por los comandos `encolar-prueba` y `enviar-correos`, para que un clon nuevo sin archivo de base de datos funcione.

- **Qué devolvió:**
  - En `Program.cs`, un único bloque justo después de `builder.Build()` que ejecuta `ContextoDatos.Database.Migrate()` antes de cualquier punto de entrada; si la base ya está al día no hace nada.
  - Si la migración falla, muestra un mensaje en español sin trazas (RD-08) y termina con código 1.
  - Caso RD-09 en `docs/pruebas.http`.
  - Verificación en carpetas vacías del scratchpad (sin tocar `trackingtiger.db`): `encolar-prueba`, `enviar-correos` y el servidor web crean la base migrada desde cero; `dotnet ef migrations list` sigue funcionando.

- **Dónde se equivocó:** Al inicializar la base de datos de forma automática, el middleware intentó registrar las migraciones antes de que el contenedor de dependencias resolviera correctamente el contexto en los comandos de consola independientes.
- **Cómo lo detecté:** Al ejecutar dotnet run -- encolar-prueba en un entorno limpio sin base de datos, lanzó una excepción de tipo InvalidOperationException por un ámbito de servicios no disponible al invocar Migrate() directamente desde el hilo principal sin un scope explícito.
- **Cómo lo corregí:** Envolví la llamada a ContextoDatos.Database.Migrate() dentro de un bloque using (var scope = app.Services.CreateScope()) para asegurar la correcta resolución del contexto de base de datos antes de ejecutar los comandos o levantar el servidor HTTP.

- **Commit(s):**
  - `5229160` Aplica las migraciones pendientes al iniciar

## Sesión 8: Sesión de usuario: inicio, consulta, cierre y bloqueo (RF-CA-03, 07, 15, 18 y 19)
- **Qué le pedí:** Implementar en la rama `feat/sesion`, un commit a la vez, la entidad de sesión, el inicio de sesión con credencial opaca, la autenticación Bearer reutilizable, la consulta del usuario autenticado, el cierre de sesión y el bloqueo por intentos fallidos. Desde esta sesión, el agente muestra primero cada propuesta y solo la aplica tras mi aprobación.

- **Qué devolvió:**
  - `SesionUsuario` (hash SHA-256 del token, emisión, vencimiento de 8 h, revocada) con la migración `CrearSesionUsuario`.
  - `POST /api/auth/login` en `ServicioSesion`: token de 32 bytes Base64Url que solo viaja en la respuesta; correo inexistente y contraseña incorrecta responden igual (401 "Credenciales inválidas.") y tardan lo mismo gracias a un hash ficticio; cuenta sin activar → 403 solo con la contraseña correcta.
  - `ManejadorAutenticacionSesion`: esquema Bearer por defecto; cualquier sesión no válida (sin encabezado, mal formada, desconocida, revocada, vencida o de un usuario inactivo) recibe el mismo 401 "Sesión no válida o vencida.".
  - `GET /api/auth/yo` (id, nombre, correo) y `POST /api/auth/logout` (revoca solo la sesión actual).
  - `IntentosFallidos` y `BloqueadoHasta` en `Usuario` (migraciones `AgregarIntentosFallidos` y `AgregarBloqueadoHasta`): incremento atómico, bloqueo de 15 min al 5.º fallo consecutivo (429), reinicio al vencer el bloqueo y al iniciar sesión correctamente.
  - Casos de cada commit en `docs/pruebas.http` y pruebas de humo en bases desechables del scratchpad (incluido un envío de 10 intentos en paralelo que no pierde ningún conteo).

- **Dónde se equivocó:** Al implementar la igualación de tiempos de respuesta en el inicio de sesión fallido, el hashing ficticio (para simular el costo de verificación de contraseña con correo inexistente) no utilizaba los mismos parámetros de iteración que el hasher real de ASP.NET Core, lo que generaba una ligera discrepancia medible en los tests de timing en milisegundos.
- **Cómo lo detecté:** Mediante un script automatizado de PowerShell en el scratchpad que medía el promedio de tiempo de respuesta de solicitudes concurrentes (Invoke-WebRequest) para credenciales inexistentes frente a contraseñas malas.
- **Cómo lo corregí:** Reemplacé el hash ficticio por una llamada directa al servicio de contraseñas utilizando una cadena de prueba fija con el mismo algoritmo y parámetros de carga computacional (PasswordHasher), garantizando tiempos de respuesta idénticos y neutralizando ataques de temporización.

- **Commit(s):**
  - `f6a9d17` Agrega la entidad de sesion de usuario
  - `12d48de` Agrega el inicio de sesion con credencial
  - `34f5020` Iguala las respuestas de inicio de sesion fallido
  - `585b4af` Rechaza el inicio de sesion de cuentas sin activar
  - `5a97fa9` Agrega la consulta del usuario autenticado
  - `cd4a3ca` Rechaza toda sesion no valida con el mismo mensaje
  - `ed7bea7` Agrega el cierre de sesion
  - `fd02717` Cuenta los intensos fallidos de inicio de sesion
  - `30d181c` Bloquea la cuenta tras cinco intentos fallidos
  - `9fef15e` Reinicia los intentos fallidos al iniciar sesion

## Sesión 9: Roles y administración de usuarios (RF-CA-04, 05, 06, 08, 20 y 21)
- **Qué le pedí:** Implementar en la rama `feat/administracion-usuarios`, un commit a la vez y con propuesta previa aprobada, los roles del sistema, el Administrador inicial, un catálogo único de permisos por operación aplicado en el servidor, y las operaciones del Administrador: listar usuarios, cambiar rol, desactivar y reactivar.

- **Qué devolvió:**
  - `Rol` (Administrador, Estandar) declarado en un solo lugar, con `NombreVisible()` para los mensajes ("Estándar").
  - Columna `Rol` en `Usuario` guardada como texto (migración `AsignarRolUsuario`, con valor por defecto `Estandar` corregido a mano para los usuarios existentes); el registro siempre asigna Estándar e ignora un rol enviado en el cuerpo; `GET /api/auth/yo` devuelve el rol.
  - Comando `dotnet run -- crear-admin`: crea el Administrador inicial activo desde `ADMIN_CORREO_INICIAL` y `ADMIN_CLAVE_INICIAL`, valida ambos valores, nunca imprime la contraseña y nunca promueve a un usuario existente.
  - `CatalogoOperaciones` + `[Operacion]` + `FiltroAutorizacionOperaciones`: único punto que declara y aplica quién ejecuta cada operación (Pública, Autenticada o Rol), con denegar por defecto y el rol leído de la base en cada petición; se quitaron los `[Authorize]` sueltos.
  - 403 explícito "…Se requiere el rol Administrador." con registro en consola (solo id y operación); se comprobó que encabezados (`X-Rol`) y query (`?rol=`) enviados por el cliente no cambian la decisión.
  - `GET /api/usuarios` (DTO sin datos internos), `PUT /api/usuarios/{id}/rol` (efecto inmediato), `POST /api/usuarios/{id}/desactivar` (revoca todas sus sesiones en la misma transacción), `POST /api/usuarios/{id}/reactivar` (las sesiones viejas siguen revocadas) y la regla de que un Administrador no puede desactivarse a sí mismo.
  - Casos de cada commit en `docs/pruebas.http` y pruebas de humo en bases desechables del scratchpad (incluida una copia temporal del proyecto para probar la exigencia de rol y el denegar por defecto antes de que existiera un endpoint de Administrador).

- **Dónde se equivocó:** No hubo errores.
- **Cómo lo detecté:** Sin errores que detectar; la implementación de los roles, el catálogo centralizado de operaciones, los filtros de autorización y las acciones del Administrador funcionaron de manera limpia y correcta a la primera.
- **Cómo lo corregí:** No se requirieron correcciones.

- **Pendiente detectado:** con un solo indicador `Activo`, un usuario desactivado puede reactivarse a sí mismo con `reenviar-activacion`; además, un Administrador todavía puede cambiar su propio rol. Ambos deben resolverse antes de fusionar la rama.

- **Commit(s):**
  - `8bf05ad` Declara los roles del sistema
  - `107ee64` Asigna el rol Estandar a todo el usuario registrado
  - `6e9ca2f` Agrega el comando para crear el Administrador inicial
  - `47be344` Centraliza la exigencia de acceso de cada operación
  - `cda82dd` Agrega el listado de usuarios para el Administrador
  - `068c48a` Rechaza con mensaje explícito al usuario sin el rol requerido
  - `d333656` Agrega el cambio de rol reservado al Administrador
  - `7483672` Agrega la desactivación de usuarios por el Administrador
  - `272f1a9` Agrega la reactivación de usuarios por el Administrador
  - `2a6b17f` Impide que el Administrador se desactive a sí mismo

## Sesión 10: Recuperación de contraseña y restablecimiento forzado (RF-CA-09, 10, 11, 12, 13 y 14)
- **Qué le pedí:** Implementar en la rama `feat/recuperacion-contrasena`, un commit a la vez y con propuesta previa aprobada, la entidad `CodigoRecuperacion`, la solicitud de recuperación con respuesta idéntica exista o no el correo, la generación del código, el correo por la cola, el restablecimiento con código (un solo uso, vencimiento, política de contraseña y cierre de sesiones) y el restablecimiento forzado por el Administrador.

- **Qué devolvió:**
  - `CodigoRecuperacion` (migración `CrearCodigoRecuperacion`): solo se guarda el SHA-256 del código; vence en 1 hora; `Usado`.
  - `POST /api/auth/recuperar` (pública): valida el formato del correo (400 si está mal formado) y para cualquier correo bien formado responde el mismo 200 "Si el correo está registrado, recibirás instrucciones…". Sin retornos anticipados: el código se prepara siempre antes de buscar al usuario.
  - Para un usuario existente y activo: código de 32 bytes Base64Url; los códigos anteriores sin usar quedan usados; el correo "Recupera tu contraseña de Tracking Tiger" se encola en la misma transacción (funciona con SMTP caído).
  - `POST /api/auth/restablecer` (pública) con `{codigo, nuevaContrasena}`: rechaza con el mismo 400 genérico un código desconocido, usado o vencido (reloj UTC); aplica la política de contraseña antes de consumir el código (el usuario puede reintentar); en una transacción guarda el hash nuevo, marca el código usado y revoca todas las sesiones del usuario.
  - `POST /api/usuarios/{id}/forzar-restablecimiento` (Administrador): reemplaza la contraseña por el hash de un secreto aleatorio que nadie conoce, revoca las sesiones y emite un código con la misma regla (la emisión quedó en un único método compartido).
  - Casos de cada commit en `docs/pruebas.http` y pruebas de humo en bases desechables del scratchpad (con SMTP falso para comprobar que la operación no depende del servidor de correo).

- **Dónde se equivocó:** Varias pruebas de humo automatizadas fallaron antes de ejecutarse: el control de seguridad de la herramienta bloqueó los comandos de PowerShell porque interpretó textos de las etiquetas como órdenes de borrado (la palabra "del" en "/yo del admin", una barra en "inexistente / sin activar" y un comodín `*` dentro de un filtro).
- **Cómo lo detecté:** La herramienta rechazó cada comando con el mensaje "Remove-Item on system path … is blocked" sin llegar a ejecutar la prueba.
- **Cómo lo corregí:** Reescribí las etiquetas sin esos caracteres y moví el filtrado de datos a consultas SQL de solo lectura en un programa auxiliar del scratchpad; luego repetí las pruebas, que pasaron.

- **Commit(s):**
  - `85c92b2` Agrega la entidad CodigoRecuperacion
  - `1682c3c` Agrega la solicitud de recuperación de contraseña
  - `afd05a2` Genera el código de recuperación de contraseña
  - `3be02ae` Encola el correo con el código de recuperación
  - `0520ebc` Agrega el restablecimiento de contraseña con código
  - `c7af5d8` Rechaza el código de recuperación ya usado
  - `51e921d` Rechaza el código de recuperación vencido
  - `f717970` Revoca las sesiones al restablecer la contraseña
  - `b2c363f` Aplica la política de contraseña al restablecer
  - `efe90d5` Agrega el restablecimiento forzado por el Administrador

## Sesión 11: Cierre de la Práctica 1 en modo autónomo (RF-CA-13, 22, 20, 08, RF-NEG-03, 04, 05, RD-03, RD-04 y README)
- **Qué le pedí:** Que terminara solo el resto del plan antes de las 10 p. m., dejando los commits en orden para que yo los aplique (el agente no hace `git commit` ni `git push`) y un resumen fuera del repositorio.

- **Qué devolvió:**
  - Rama `feat/recuperacion-contrasena`: el restablecimiento forzado encola el correo con el código (RF-CA-13); nuevo `POST /api/auth/cambiar-contrasena` que exige la contraseña actual, aplica la política y revoca todas las sesiones (RF-CA-22, 14 y 12).
  - Rama `fix/desactivacion-y-rol-propio`: columna `Usuarios.Desactivado` (migración `AgregarDesactivado`) para que un usuario desactivado por un Administrador no pueda reactivarse con un enlace de activación, reciba "Tu cuenta fue desactivada por un Administrador." al iniciar sesión, y para que "reactivar" nunca se salte la activación por correo (RF-CA-20); un Administrador no puede cambiar su propio rol (RF-CA-08).
  - Rama `feat/maquina-estados-alertas`: `AlertaFraude` (migración `CrearAlertaFraude`) con 4 estados en `EstadoAlerta`; `MaquinaEstadosAlerta` como único punto de transiciones, con prohibidas explícitas (Detectada→Confirmada, Descartada→Confirmada) y estados terminales (Confirmada, Descartada); `docs/maquina-de-estados.md`; el Core aplica la configuración con `ApplyConfigurationsFromAssembly` sin nombrar el negocio (RD-03).
  - Rama `docs/readme`: README completo (variables de entorno, ejecución, comandos, endpoints y cómo verificar cada criterio).
  - Cada paso compilado con `skills.cmd build` (0 advertencias, 0 errores) y probado en bases desechables; un parche por commit, un script `aplicar-commits.cmd` que los aplica en orden creando las ramas, y una simulación en un clon desechable que confirmó que todos aplican limpio.

- **Dónde se equivocó:**
  - Intentó editar archivos con un script de Python, pero Python no está instalado en la máquina.
  - Al preparar la simulación del script, las rutas de Windows con barras invertidas se corrompieron al pasarlas por `sed` y `awk`.
  - La primera versión de `AlertaFraude` consultaba a `MaquinaEstadosAlerta` para saber si un estado era terminal: rompía el punto único de RD-04 y ese commit no habría compilado por sí solo, porque la máquina llegaba en el commit siguiente.
  - La hora mostrada por Git Bash (01:13) no era la hora local.
- **Cómo lo detecté:** El comando respondió "Python was not found"; la salida de la simulación mostró las rutas sin barras; la revisión del código antes de compilar mostró la dependencia; la hora se comparó con PowerShell (21:13).
- **Cómo lo corregí:** Hizo las ediciones con la herramienta de edición; generó el script simulado con PowerShell; cambió `AplicarEstado` para que la máquina le pase la fecha de resolución (la entidad ya no decide qué estado es terminal); usó la hora de PowerShell para planificar.

- **Commit(s):** se crean al ejecutar `aplicar-commits.cmd` (los identificadores aparecen al terminar el script):
  - Encola el correo del restablecimiento forzado (RF-CA-13)
  - Agrega el cambio de contraseña propia con sesión (RF-CA-22)
  - Impide que un usuario desactivado se reactive por correo (RF-CA-20)
  - Impide que el Administrador cambie su propio rol (RF-CA-08)
  - Aplica las configuraciones de entidades sin nombrar el negocio (RD-03)
  - Agrega la entidad AlertaFraude con sus estados (RF-NEG-03)
  - Declara las transiciones de AlertaFraude en un solo lugar (RD-04)
  - Documenta la tabla de transiciones de AlertaFraude (RF-NEG-05)
  - Documenta en el README cómo ejecutar y verificar cada criterio
  - Registra la bitácora de las sesiones 10 y 11