# Tracking Tiger

Sistema antifraude para tarjetas de débito y crédito. API web en ASP.NET Core (.NET 10) con EF Core + SQLite.

- **Core** (`Core/`): Control de acceso (registro, activación por correo, sesión, roles, administración de usuarios, recuperación de contraseña) y la cola mínima de correos.
- **Negocio** (`Negocio/`): alertas de fraude con su máquina de estados (ver `docs/maquina-de-estados.md`). El Core no depende del Negocio (RD-03).

Todos los casos de prueba manuales, con la petición y el resultado esperado, están en **`docs/pruebas.http`** (se ejecuta con la extensión REST Client de VS Code o copiando las peticiones a cualquier cliente HTTP).

## Requisitos

- [Git](https://git-scm.com/downloads)
- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) (el proyecto usa `net10.0`; .NET 8 no sirve). Comprobar con `dotnet --list-sdks`: debe aparecer una línea que empiece con `10.0`.
- Opcional: [DB Browser for SQLite](https://sqlitebrowser.org/) para leer la base de datos `trackingtiger.db`.
- Una cuenta SMTP para que el correo llegue de verdad (por ejemplo Gmail con una *contraseña de aplicación*).

## Clonar

```
git clone https://github.com/greilenny26/Tracking-Tiger.git
cd Tracking-Tiger
git checkout practica-1
```

## Variables de entorno

Las credenciales **nunca** están en el repositorio (RD-10): se leen de variables de entorno. `.env.example` lista los nombres; los valores van en un archivo local `.env.cmd`, que está ignorado por Git.

| Variable | Para qué sirve | ¿Obligatoria? |
|---|---|---|
| `SMTP_HOST` | Servidor SMTP que usa el enviador de la cola (por ejemplo `smtp.gmail.com`). | Para `enviar-correos` |
| `SMTP_PORT` | Puerto SMTP (587 STARTTLS o 465 SSL). Vacío → 587. | No |
| `SMTP_USUARIO` | Usuario de la cuenta SMTP. | Para `enviar-correos` |
| `SMTP_CLAVE` | Contraseña de aplicación de la cuenta SMTP. | Para `enviar-correos` |
| `SMTP_REMITENTE` | Dirección que aparece como remitente. Vacío → `SMTP_USUARIO`. | No |
| `APP_URL_BASE` | URL base de la API para armar el enlace de activación, por ejemplo `http://localhost:5000`. | Para registrar usuarios |
| `ADMIN_CORREO_INICIAL` | Correo del primer Administrador (comando `crear-admin`). | Para `crear-admin` |
| `ADMIN_CLAVE_INICIAL` | Contraseña del primer Administrador (mínimo 8 caracteres, con letras y números). | Para `crear-admin` |

Crear `.env.cmd` en la raíz del proyecto (formato CMD, sin espacios alrededor del `=`):

```
set SMTP_HOST=smtp.gmail.com
set SMTP_PORT=587
set SMTP_USUARIO=tu-cuenta@gmail.com
set SMTP_CLAVE=tu-contraseña-de-aplicación
set SMTP_REMITENTE=tu-cuenta@gmail.com
set APP_URL_BASE=http://localhost:5000
set ADMIN_CORREO_INICIAL=admin@tu-dominio.com
set ADMIN_CLAVE_INICIAL=UnaClaveSegura1
```

En **cada terminal CMD** donde se ejecute el proyecto, cargarlas primero:

```
call .env.cmd
```

(En PowerShell `call` no existe: usar una terminal CMD.)

## Ejecutar

```
call .env.cmd
dotnet run
```

- Al iniciar se aplican automáticamente todas las migraciones pendientes (RD-09): un clon nuevo crea `trackingtiger.db` desde cero, sin pasos manuales.
- La API queda en `http://localhost:5000` (la consola muestra `Now listening on: http://localhost:5000`). Para detenerla: `Ctrl+C`.
- Los datos persisten en `trackingtiger.db`: reiniciar la aplicación no pierde usuarios (RD-09).

### Comandos aparte (no levantan el servidor web)

| Comando | Qué hace |
|---|---|
| `dotnet run -- crear-admin` | Crea el primer Administrador (activo) con `ADMIN_CORREO_INICIAL` / `ADMIN_CLAVE_INICIAL`. Si el correo ya existe no cambia nada. |
| `dotnet run -- enviar-correos` | Proceso independiente que envía por SMTP los correos pendientes de la cola y los marca como enviados (RF-NOT-09). Ejecutarlo dos veces no duplica envíos (RF-NOT-12). |
| `dotnet run -- encolar-prueba <correo>` | Encola un correo de prueba (solo para probar la cola). |

**Importante:** la API nunca envía correos dentro de la operación (RF-NOT-08). Después de registrarse, pedir recuperación o forzar un restablecimiento, ejecutar `dotnet run -- enviar-correos` en **otra** terminal CMD (con `call .env.cmd`) para que el correo llegue.

## Endpoints

Quién puede ejecutar cada operación está declarado en un solo lugar: `Core/ControlAcceso/CatalogoOperaciones.cs` (RF-CA-05). La verificación ocurre en el servidor en cada petición (RD-06). La credencial de sesión se envía como `Authorization: Bearer <token>`.

| Método y ruta | Acceso | Cuerpo |
|---|---|---|
| `POST /api/auth/registro` | Pública | `{"nombre","correo","contrasena"}` |
| `GET /api/auth/activar?token=...` | Pública | — |
| `POST /api/auth/reenviar-activacion` | Pública | `{"correo"}` |
| `POST /api/auth/login` | Pública | `{"correo","contrasena"}` → `{"token","venceEn"}` |
| `GET /api/auth/yo` | Con sesión | — |
| `POST /api/auth/logout` | Con sesión | — |
| `POST /api/auth/cambiar-contrasena` | Con sesión | `{"contrasenaActual","nuevaContrasena"}` |
| `POST /api/auth/recuperar` | Pública | `{"correo"}` |
| `POST /api/auth/restablecer` | Pública | `{"codigo","nuevaContrasena"}` |
| `GET /api/usuarios` | Administrador | — |
| `PUT /api/usuarios/{id}/rol` | Administrador | `{"rol"}` (`Administrador` o `Estandar`) |
| `POST /api/usuarios/{id}/desactivar` | Administrador | — |
| `POST /api/usuarios/{id}/reactivar` | Administrador | — |
| `POST /api/usuarios/{id}/forzar-restablecimiento` | Administrador | — |

## Cómo verificar cada criterio

Pasos en el orden de la revisión. Las peticiones exactas y los resultados esperados están en `docs/pruebas.http` (sección indicada entre paréntesis).

### Preparación

1. Terminal A (CMD): `call .env.cmd` y `dotnet run`.
2. Terminal B (CMD): `call .env.cmd` y `dotnet run -- crear-admin` → "Se creó el Administrador inicial…". Este usuario es el Administrador.

### Registro y activación (RF-CA-01, 02, 14, 15, 16, 17)

1. Registrarse con un correo propio: `POST /api/auth/registro` → **201** "La cuenta se creó, pero todavía no está activa…" (RF-CA-01).
2. Iniciar sesión antes de activar: `POST /api/auth/login` con la contraseña correcta → **403** "La cuenta no está activa. Revisa tu correo para activarla." (RF-CA-15).
3. Terminal B: `dotnet run -- enviar-correos` → llega el correo "Activa tu cuenta de Tracking Tiger" con el enlace.
4. Abrir el enlace → **200** "Tu cuenta se activó correctamente…". Ahora el login funciona (RF-CA-16).
5. Abrir el enlace por segunda vez → **400** "Este enlace de activación ya fue usado." y nada cambia (RF-CA-16). Un enlace vencido → "El enlace de activación ya venció."
6. Registrar el mismo correo otra vez (con mayúsculas o espacios también) → **409** "Ya existe una cuenta registrada con ese correo." (RF-CA-01).
7. Contraseña de 5 caracteres → **400** "La contraseña debe tener al menos 8 caracteres."; correo mal formado → **400** "El correo no tiene un formato válido." (RF-CA-14, RD-07).
8. Reenvío del enlace: `POST /api/auth/reenviar-activacion` con un correo existente y con uno inexistente → la **misma** respuesta 200. El reenvío invalida el enlace anterior (RF-CA-17).
9. Leer el almacenamiento en DB Browser (`Usuarios.HashContrasena`): la contraseña no aparece (`PBKDF2-SHA256$600000$sal$hash`) y dos usuarios con la misma contraseña tienen valores distintos (RF-CA-02, RD-05).

### Sesión (RF-CA-03, 07, 18, 19)

1. Login con contraseña incorrecta y con correo inexistente → **mismo** 401 "Credenciales inválidas." y tiempos parecidos (RF-CA-03).
2. `GET /api/auth/yo` con el token → **200** con id, nombre, correo y rol; sin token o con uno inválido → **401** "Sesión no válida o vencida." (RF-CA-07).
3. `POST /api/auth/logout` y volver a usar ese token → **401** (RF-CA-18).
4. Fallar el login 5 veces seguidas y luego usar la contraseña correcta → **429** "Cuenta bloqueada temporalmente…" durante 15 minutos. Un login correcto antes del 5.º fallo pone el contador en cero (RF-CA-19). En DB Browser: `Usuarios.IntentosFallidos` y `BloqueadoHasta`.

### Roles y administración (RF-CA-04, 05, 06, 08, 20, 21)

1. Todo usuario tiene exactamente un rol (`Usuarios.Rol`: `Administrador` o `Estandar`); el registro siempre asigna Estándar (RF-CA-04).
2. Con sesión de Estándar, construir a mano `GET /api/usuarios` (también con `X-Rol: Administrador` o `?rol=Administrador`) → **403** "No tienes permiso para ejecutar esta operación. Se requiere el rol Administrador." (RF-CA-06, RD-06).
3. Con sesión de Estándar, intentar cambiar el propio rol: `PUT /api/usuarios/{mi-id}/rol` → **403** (RF-CA-08).
4. Como Administrador:
   - `GET /api/usuarios` → lista con id, nombre, correo, rol y activo; nunca hashes ni tokens (RF-CA-21).
   - `PUT /api/usuarios/{id}/rol` con `{"rol":"Administrador"}` → **200**; surte efecto en la siguiente petición de ese usuario (RF-CA-08). Cambiar el propio rol → **400**.
   - Desactivar un usuario con sesión abierta: `POST /api/usuarios/{id}/desactivar` → **200**; su token deja de servir (**401**) y no puede iniciar sesión (**403** "Tu cuenta fue desactivada por un Administrador."). No puede reactivarse solo con un enlace de activación (RF-CA-20).
   - Intentar desactivarse a sí mismo → **400** "No puedes desactivar tu propia cuenta." (RF-CA-20).
   - `POST /api/usuarios/{id}/reactivar` → **200**; sus sesiones viejas siguen revocadas.

### Contraseñas (RF-CA-09, 10, 11, 12, 13, 22)

1. `POST /api/auth/recuperar` con un correo inexistente y con uno existente → **misma** respuesta 200 "Si el correo está registrado…" (RF-CA-09).
2. Terminal B: `dotnet run -- enviar-correos` → llega el correo con el código (vence en 1 hora, un solo uso) (RF-CA-10).
3. `POST /api/auth/restablecer` con `{"codigo","nuevaContrasena"}` → **200**. Usar el mismo código otra vez → **400** "El código no es válido, ya fue usado o venció." y la contraseña no cambia (RF-CA-10, 11).
4. Login con la contraseña vieja → **401**; con la nueva → **200** (RF-CA-11).
5. Un token emitido antes del restablecimiento → **401** en `GET /api/auth/yo` (RF-CA-12).
6. Como Administrador: `POST /api/usuarios/{id}/forzar-restablecimiento` → **200**; la contraseña anterior deja de servir, sus sesiones se cierran y le llega por la cola el correo con el código (RF-CA-13).
7. Con sesión: `POST /api/auth/cambiar-contrasena` con una contraseña actual incorrecta → **400** "La contraseña actual no es correcta."; con la correcta → **200** y todas las sesiones se cierran (RF-CA-22, 14, 12).

### Correo por cola (RF-NOT-08, 09, 12, 13)

1. Apagar el acceso a SMTP (por ejemplo, terminal sin `call .env.cmd` o con `set SMTP_HOST=localhost` y `set SMTP_PORT=2599`) y registrar un usuario → **201** de inmediato; en DB Browser el correo queda en `CorreosEnCola` con `Estado = Pendiente` (RF-NOT-08).
2. Con las variables correctas: `dotnet run -- enviar-correos` → el correo llega y pasa a `Enviado` (RF-NOT-09). Ejecutarlo otra vez → "No hay correos pendientes." y no se duplica nada (RF-NOT-12).
3. Las credenciales SMTP solo vienen de variables de entorno; no hay ninguna en el repositorio ni en el historial (RF-NOT-13, RD-10).

### Persistencia (RD-09)

Detener la API (`Ctrl+C`) y volver a ejecutar `dotnet run`: los usuarios siguen ahí.

### Máquina de estados del negocio (RF-NEG-03, 04, 05, RD-04)

- Estados en un solo lugar: `Negocio/Alertas/EstadoAlerta.cs`.
- Transiciones en un solo lugar, con prohibidas explícitas y estados terminales: `Negocio/Alertas/MaquinaEstadosAlerta.cs`.
- Tabla de transiciones: `docs/maquina-de-estados.md`.

## Estructura

```
Core/                 Control de acceso, correo, persistencia y web (no depende de Negocio)
  ControlAcceso/      Usuarios, sesiones, roles, catálogo de operaciones, recuperación
  Correo/             Cola de correos y enviador SMTP
  Persistencia/       ContextoDatos (EF Core + SQLite)
  Web/                Manejo global de errores (RD-08)
Negocio/Alertas/      AlertaFraude y su máquina de estados
Migrations/           Migraciones de EF Core (se aplican solas al iniciar)
docs/                 pruebas.http, maquina-de-estados.md, requisitos y bitácora
```

## Notas

- `bin/`, `obj/`, `trackingtiger.db` y `.env.cmd` están ignorados por Git.
- Ningún mensaje al usuario expone trazas, rutas ni consultas (RD-08); los errores inesperados responden 500 con un mensaje genérico.
- Todas las fechas se guardan en UTC desde un único reloj (`IReloj`, RD-11).
