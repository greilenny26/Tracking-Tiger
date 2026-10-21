# Bitácora de sesión con el agente — Asignación 2

Agente: Claude (Claude Code, app de escritorio).
Pieza: Gestión de permisos (RF-GP-01 a RF-GP-07) y su primera suite de pruebas.

> Borrador en construcción: se completa a medida que avanza la sesión.

## Tarea 1 — Reorganizar el repositorio para alojar las pruebas (RD-12)

**Fecha:** 17 de octubre de 2026.

### Qué le pedí
Que me guiara paso a paso para separar la aplicación de las pruebas. La aplicación
estaba en la raíz del repositorio (`Tracking Tiger.csproj`, `Core/`, `Negocio/`…), y un
proyecto de pruebas dentro de esa carpeta sería compilado también por la aplicación.

### Qué me devolvió
- Propuso la estructura `src/TrackingTiger/` (aplicación) + `tests/TrackingTiger.Pruebas/`
  (pruebas) + `TrackingTiger.sln` en la raíz, para que `dotnet test` ejecute toda la suite
  con un solo comando.
- Los comandos para hacerlo yo misma: rama `refactor/estructura-src-tests`, `git mv` de cada
  carpeta (para conservar el historial; `git status` mostró los archivos como `R`, renombrados),
  `dotnet new sln --format sln` y `dotnet sln add`.
- `dotnet build` compiló correctamente desde la raíz.

### Error del agente: la ubicación de la base de datos
**Qué afirmó:** que después de mover el proyecto, `dotnet run --project src/TrackingTiger`
seguiría usando la base de datos `trackingtiger.db` de la raíz, porque la ruta relativa se
resolvía desde la carpeta donde se ejecuta el comando.

**Cómo lo detecté:** al ejecutar la aplicación, la salida mostró que se aplicaban **todas**
las migraciones desde la primera, algo que no debía pasar con una base de datos existente:

```
info: Microsoft.EntityFrameworkCore.Migrations[20402]
      Applying migration '20261003174701_CrearCorreoEnCola'.
...
info: Microsoft.Hosting.Lifetime[0]
      Content root path: C:\Users\Greilenny\Tracking-Tiger-Proyecto\Tracking-Tiger\src\TrackingTiger
```

Al buscar los archivos aparecieron dos bases de datos: la original en la raíz (4 de octubre,
intacta) y una nueva y vacía en `src/TrackingTiger/`.

**Por qué ocurrió:** `dotnet run` usa el perfil de `Properties/launchSettings.json`, que
ejecuta la aplicación desde la carpeta del proyecto. La cadena fija
`"Data Source=trackingtiger.db"` en `Program.cs` es relativa, así que se resolvió dentro de
`src/TrackingTiger/` y SQLite creó un archivo nuevo.

**Cómo lo corregí:** escribí en `Program.cs` la lectura de la variable de entorno `DB_RUTA`
(lo mismo que el maestro señaló en el feedback de la asignación 1). Si la variable no existe,
la ruta por defecto ya no es relativa: se arma con `builder.Environment.ContentRootPath`, así que
siempre es `src/TrackingTiger/trackingtiger.db`, sin importar desde dónde se ejecute.

```csharp
var rutaBaseDatos = Environment.GetEnvironmentVariable("DB_RUTA");
if (string.IsNullOrWhiteSpace(rutaBaseDatos))
    rutaBaseDatos = Path.Combine(builder.Environment.ContentRootPath, "trackingtiger.db");
```

Lo comprobé ejecutando la aplicación dos veces (18 de octubre):

- **Sin `DB_RUTA`:** encontró la base de datos de `src/TrackingTiger/` y no aplicó migraciones.
  ```
  info: Microsoft.EntityFrameworkCore.Migrations[20405]
        No migrations were applied. The database is already up to date.
  ```
- **Con `set DB_RUTA=%TEMP%\prueba-tracking.db`:** creó una base de datos nueva en esa ruta y
  aplicó todas las migraciones desde la primera.
  ```
  info: Microsoft.EntityFrameworkCore.Migrations[20402]
        Applying migration '20261003174701_CrearCorreoEnCola'.
  ```

**Qué aprendí:** no basta con que el agente diga "va a funcionar"; hay que mirar la salida real.
La pista estuvo en dos líneas del registro (`Applying migration` y `Content root path`).

### Segundo error del agente: documentar `tests/` antes de que existiera
**Qué hizo:** en el paso de actualizar el README me dio el bloque de la sección "Estructura" que ya
mencionaba `tests/` (pruebas automatizadas), aunque esa carpeta todavía no existía: él mismo había
dicho que el proyecto de pruebas "lo creamos después".

**Cómo lo detecté:** antes de hacer el commit abrí la carpeta del repositorio en el explorador y comparé
con lo que decía el README: no había ninguna carpeta `tests`. Como el maestro sigue el README al pie de
la letra, habría descrito algo que no estaba en el tag.

**Cómo lo corregí:** le pregunté dónde estaba esa carpeta; reconoció que el orden de los pasos estaba mal
y cambiamos el orden: primero creamos el proyecto de pruebas (`dotnet new xunit`, `dotnet sln add`,
`dotnet add reference`) y solo después hice el commit del README con la estructura nueva.

## Tarea 2 — Construir la pieza Gestión de permisos (RF-GP-01 a RF-GP-07)

**Fechas:** 18 y 20 de octubre de 2026.

### Qué le pedí
Que me guiara para construir la pieza. Cuando me propuso que "permiso" fuera una operación concreta del
catálogo (en vez de dar el rol de Administrador completo), le pedí agregar algo más: que el Administrador
elija por cuánto tiempo dura el permiso y que al vencer se revoque solo.

### Qué me devolvió
- Me advirtió que la vigencia **no** podía ser un estado nuevo de la solicitud (`Aplicada → Revocada`),
  porque la tabla del maestro dice que ninguna otra transición está permitida. Lo separamos en dos cosas:
  la solicitud (Pendiente / Aprobada / Aplicada / Rechazada) y el `PermisoOtorgado`, con fecha de
  vencimiento, que el filtro de autorización revisa en cada petición.
- `MaquinaEstadosSolicitud.IntentarTransicion`, el único punto que decide las transiciones (RD-04), y
  encima de ella el servicio y los endpoints de crear, consultar, aprobar y rechazar.
- Las pruebas de `tests/TrackingTiger.Pruebas/Permisos/MaquinaEstadosSolicitudPruebas.cs` (10 casos).

### Un error mío que el agente detectó
Al registrar el servicio en `Program.cs` me salté ese paso: el commit compiló, pero el agente revisó con
`git show --stat` y vio que `Program.cs` no estaba en el commit. Sin esa línea, los endpoints de permisos
responderían 500. Lo agregué en su propio commit ("Registra el servicio de solicitudes de permiso").

### Verificación
`dotnet test` → `Passed: 10, Failed: 0`. Además se ejecutó el recorrido completo del README contra una
base de datos de prueba: 403 antes de pedir el permiso, 201 al crear, 200 al aprobar con
`"aprobadorId":1` y `permisoVigenteHasta`, 200 en `GET /api/usuarios` con el permiso, 403 otra vez al
vencer (1 minuto), 400 al aprobar dos veces ("La solicitud ya está en Aplicada…"), 400 al rechazar sin
motivo, 400 al resolver la propia solicitud y la solicitud pendiente seguía ahí tras reiniciar la API.
