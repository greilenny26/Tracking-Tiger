# Contexto del agente: Tracking Tiger

El agente que se usa en este repositorio es Claude Code; este es su archivo de contexto.

## Propósito
Sistema antifraude para tarjetas de débito y crédito, proyecto de Programación III (ITLA).
Tiene un **Core** común (control de acceso, gestión de permisos, documentos, notificaciones,
cola de correos, reportes) y un **módulo de negocio** (Tarjetas, con su máquina de estados).

## Lenguaje y framework
- C# sobre .NET 10 (`net10.0`), ASP.NET Core Web API.
- EF Core + SQLite. MailKit para SMTP. xUnit para las pruebas.
- Docker Compose para levantarlo; SeaweedFS guarda los archivos.

## Cómo correr
Desde la raíz del repositorio. `.env` lo crea la persona copiando `.env.example`:

```
docker compose up --build
```

La API responde en `http://localhost:5000`. Sin Docker: ver la sección «Ejecutar» del README.

## Cómo probar
```
dotnet test
```
No necesita variables de entorno ni la aplicación levantada. Las peticiones manuales están en `docs/pruebas.http`.

## Estructura
- `src/TrackingTiger/Core/<Pieza>/`: una carpeta por pieza del Core.
- `src/TrackingTiger/Negocio/Tarjetas/`: módulo de negocio. El Core NUNCA referencia a Negocio (RD-03).
- `src/TrackingTiger/Migrations/`: migraciones de EF Core, se aplican solas al iniciar.
- `tests/TrackingTiger.Pruebas/`: pruebas, una carpeta por pieza.
- `docs/requisitos/`: requisitos exactos. Léelos antes de implementar.

## Convenciones de código
- Clases, métodos, propiedades, mensajes al usuario y comentarios en español.
- La lógica de negocio no va en los controladores (RD-02).
- Quién puede ejecutar cada operación se declara solo en `Core/ControlAcceso/CatalogoOperaciones.cs`
  y se valida en el servidor (RF-CA-05, RD-06).
- El id del usuario sale siempre de la sesión, nunca de la petición.
- Toda fecha se toma de `IReloj`, en UTC (RD-11).
- Toda entrada externa se valida (RD-07). Los errores no exponen trazas, rutas ni consultas (RD-08).
- Contraseñas con hash y sal. De los tokens (activación, recuperación, sesión) se guarda solo el hash.
- Respuestas idénticas exista o no el correo (activación, recuperación); mismo mensaje en login fallido.
- Los correos nunca se envían dentro de la operación: se registran en `CorreoEnCola` y los envía un proceso aparte.

## Restricciones: lo que el agente no puede hacer
- **Nunca leer, imprimir, copiar ni modificar `.env` ni `.env.cmd`.**
- **Nunca escribir credenciales ni claves** en el código, los documentos, las pruebas o los commits.
  Se leen de variables de entorno (RD-10). Cada variable nueva se agrega a `.env.example` con un valor de ejemplo.
- No ejecutar `git commit`, `git push` ni crear etiquetas: eso lo hace la dueña del repositorio.
- No modificar ni borrar etiquetas, ramas entregadas ni migraciones ya existentes en `Migrations/`.
- No cambiar las máquinas de estados (`MaquinaEstadosSolicitud`, `MaquinaEstadosTarjeta`) ni sus tablas
  sin que se pida expresamente.
- No cambiar el comportamiento del Control de acceso, Gestión de permisos ni Documentos salvo para
  conectar una notificación o un registro de auditoría; sus pruebas existentes deben seguir pasando sin modificarse.
- No agregar paquetes NuGet sin preguntar.
- No avanzar a otra funcionalidad sin que se pida.

## Reglas de commits (el profesor las califica)
- Nunca trabajar directo en `main`. Una rama por funcionalidad: `feat/<nombre>`.
- Un commit = un caso = un solo ID de requisito en el asunto.
- Asunto en español, imperativo, sin " y ", " e ", " o " ni "/". Formato: `Verbo + qué (RF-XX-NN)`.
- No mezclar cambios sin relación en una misma tarea.

## Antes de dar algo por terminado
1. Compila (`dotnet build`) y `dotnet test` pasa.
2. Cada criterio de `docs/criterios-*.md` que aplique está comprobado.
3. Cada tarea agrega su caso a `docs/pruebas.http` (petición + resultado esperado como comentario).
4. Se listan los archivos tocados y cómo probar el cambio.

## Habilidades disponibles
- `skills.cmd build`: compila con `dotnet build`.
- `skills.cmd test`: ejecuta las pruebas.
- `skills.cmd check-secrets`: busca credenciales expuestas.
- `skills.cmd clean`: limpia ejecutables y temporales.
- `skills.cmd db-migrate <nombre>`: crea y aplica una migración de EF Core.
- `skills.cmd git-feature <nombre>`: crea la rama `feat/<nombre>`.
