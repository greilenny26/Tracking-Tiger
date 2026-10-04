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