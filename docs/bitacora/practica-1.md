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