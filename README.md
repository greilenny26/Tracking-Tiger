# Tracking Tiger

Sistema antifraude para tarjetas de débito y crédito. API web en ASP.NET Core (.NET 10) con EF Core + SQLite.

- **Core** (`src/TrackingTiger/Core/`): Control de acceso (registro, activación por correo, sesión, roles, administración de usuarios, recuperación de contraseña) y la cola mínima de correos.
- **Negocio** (`src/TrackingTiger/Negocio/Tarjetas/`): tarjetas de crédito vigiladas por el sistema antifraude, con su máquina de estados Activa / BloqueoSuave / BloqueoTotal / Cancelada (ver `docs/maquina-de-estados.md`). El Core no depende del Negocio (RD-03).

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
git checkout practica-4
```

## Contenedor

Es la forma recomendada de levantar el proyecto. Solo necesita [Docker](https://www.docker.com/products/docker-desktop/) (no hace falta instalar .NET).

**1. Copiar el archivo de ejemplo** (es el único paso previo), desde la raíz del repositorio:

```
cp .env.example .env
```

En CMD de Windows: `copy .env.example .env`.

**2. Levantar el proyecto:**

```
docker compose up --build
```

La API responde en **http://localhost:5000**. Sin sesión, `http://localhost:5000/api/documentos` debe responder 401 con `{"mensaje":"Sesión no válida o vencida."}`.

**3. Detenerlo:** `Ctrl+C` en esa terminal, o desde otra:

```
docker compose down
```

Los datos sobreviven a `docker compose down` y a volver a levantar. `docker compose down -v` borra los volúmenes y con ellos **todos** los datos.

### Qué se levanta

| Servicio | Qué hace | Puerto en el equipo |
|---|---|---|
| `app` | La API. Al arrancar aplica las migraciones y crea el Administrador inicial si no existe. | 5000 |
| `correo` | Proceso aparte que envía la cola de correos cada 15 segundos (RF-NOT-09). | Ninguno |
| `seaweedfs` | Almacén de los archivos de los documentos (RF-DOC-05). | Ninguno |

| Volumen | Qué guarda |
|---|---|
| `basedatos` | La base de datos SQLite: usuarios, solicitudes, tarjetas y registros de documentos (RD-09). |
| `documentos` | Los archivos de los documentos (RF-DOC-05). |

### Usuario Administrador

Se crea solo, con los valores de `.env`. Con el `.env.example` sin cambios: correo `admin@ejemplo.test`, contraseña `CambiaEsta123`.

### Correos: enlace de activación y código de recuperación

Con el `.env.example` sin cambios, `SMTP_HOST=consola`: los correos **no se envían**, se escriben en el registro del servicio `correo` (solo para pruebas). Unos segundos después de registrar un usuario o de pedir la recuperación de contraseña, ejecutar:

```
docker compose logs correo
```

Ahí aparecen el enlace de activación y el código de recuperación. Para enviar correos de verdad, poner los datos de un servidor SMTP en `.env` y reiniciar.

Con el proyecto en contenedores, en las secciones siguientes no hacen falta `.env.cmd`, `dotnet run`, `crear-admin` ni `enviar-correos`.

### Documentos (RF-DOC-01 a 05)

Todas las operaciones exigen sesión. Con `TOKEN` igual al `token` que devuelve `POST /api/auth/login`:

```
curl -H "Authorization: Bearer TOKEN" -F "archivo=@nota.txt;type=text/plain" http://localhost:5000/api/documentos
curl -H "Authorization: Bearer TOKEN" http://localhost:5000/api/documentos
curl -H "Authorization: Bearer TOKEN" http://localhost:5000/api/documentos/1/descargar
curl -X DELETE -H "Authorization: Bearer TOKEN" http://localhost:5000/api/documentos/1
```

Otro usuario Estándar recibe 403 al descargar o eliminar ese documento, y no lo ve en su listado. El Administrador ve, descarga y elimina los de todos. La eliminación es lógica: la fila sigue en la tabla `Documentos` con `Eliminado = 1`. Casos completos en `docs/pruebas.http`.

**Persistencia:** subir un documento, ejecutar `docker compose down` (sin `-v`) y `docker compose up`: el documento sigue en el listado, se descarga y el usuario inicia sesión.

## Variables de entorno

Las credenciales **nunca** están en el repositorio (RD-10): se leen de variables de entorno. `.env.example` lista todas con un valor de ejemplo; los valores reales van en un archivo local que está ignorado por Git: `.env` con Docker (ver «Contenedor»), o `.env.cmd` para ejecutar sin Docker, como se explica a continuación.

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
| `DB_RUTA` | Ruta del archivo de la base de datos SQLite. Vacío → `src\TrackingTiger\trackingtiger.db`. | No |
| `SEAWEEDFS_URL` | URL del Filer de SeaweedFS, donde se guardan los archivos de los documentos. | Para subir y descargar documentos |
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
dotnet run --project src/TrackingTiger
```

- Al iniciar se aplican automáticamente todas las migraciones pendientes (RD-09): un clon nuevo crea `trackingtiger.db` desde cero, sin pasos manuales.
- La API queda en `http://localhost:5000` (la consola muestra `Now listening on: http://localhost:5000`). Para detenerla: `Ctrl+C`.
- Los datos persisten en `src\TrackingTiger\trackingtiger.db`, o en la ruta indicada en `DB_RUTA`. La ruta no depende de la carpeta desde donde se ejecute el comando: reiniciar la aplicación no pierde datos (RD-09).

### Comandos aparte (no levantan el servidor web)

| Comando | Qué hace |
|---|---|
| `dotnet run --project src/TrackingTiger -- crear-admin` | Crea el primer Administrador (activo) con `ADMIN_CORREO_INICIAL` / `ADMIN_CLAVE_INICIAL`. Si el correo ya existe no cambia nada. |
| `dotnet run --project src/TrackingTiger -- enviar-correos` | Proceso independiente que envía por SMTP los correos pendientes de la cola y los marca como enviados (RF-NOT-09). Ejecutarlo dos veces no duplica envíos (RF-NOT-12). |
| `dotnet run --project src/TrackingTiger -- encolar-prueba <correo>` | Encola un correo de prueba (solo para probar la cola). |

**Importante:** la API nunca envía correos dentro de la operación (RF-NOT-08). Después de registrarse, pedir recuperación o forzar un restablecimiento, ejecutar `dotnet run --project src/TrackingTiger -- enviar-correos` en **otra** terminal CMD (con `call .env.cmd`) para que el correo llegue.

## Endpoints

Quién puede ejecutar cada operación está declarado en un solo lugar: `src/TrackingTiger/Core/ControlAcceso/CatalogoOperaciones.cs` (RF-CA-05). La verificación ocurre en el servidor en cada petición (RD-06). La credencial de sesión se envía como `Authorization: Bearer <token>`.

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
| `POST /api/permisos/solicitudes` | Estándar | `{"permiso"}` (valores permitidos: `listar-usuarios`, `levantar-bloqueo-total`) |
| `GET /api/permisos/solicitudes/pendientes` | Administrador | — |
| `GET /api/permisos/solicitudes/mias` | Con sesión | — |
| `POST /api/permisos/solicitudes/{id}/aprobar` | Administrador | `{"minutosVigencia"}` (1 a 43200) |
| `POST /api/permisos/solicitudes/{id}/rechazar` | Administrador | `{"motivo"}` |
| `POST /api/tarjetas` | Con sesión | `{"titular","ultimosCuatro","limiteCredito"}` |
| `POST /api/tarjetas/{id}/transacciones` | Con sesión | `{"monto","comercio"}` |
| `GET /api/tarjetas/{id}` | Con sesión | — |
| `POST /api/tarjetas/{id}/reconocer` | Estándar | — |
| `POST /api/tarjetas/{id}/no-reconocer` | Estándar | `{"nota"}` |
| `POST /api/tarjetas/{id}/levantar-bloqueo-total` | Estándar + permiso `levantar-bloqueo-total` | `{"nota"}` |
| `POST /api/tarjetas/{id}/cancelar` | Estándar | `{"nota"}` |
| `GET /api/tarjetas/{id}/historial` | Administrador | — |

`GET /api/usuarios` también lo puede ejecutar un usuario Estándar con un permiso `listar-usuarios` aprobado y vigente (RF-GP-05).

## Cómo verificar cada criterio

Pasos en el orden de la revisión. Las peticiones exactas y los resultados esperados están en `docs/pruebas.http` (sección indicada entre paréntesis).

### Preparación

1. Terminal A (CMD): `call .env.cmd` y `dotnet run --project src/TrackingTiger`.
2. Terminal B (CMD): `call .env.cmd` y `dotnet run --project src/TrackingTiger -- crear-admin` → "Se creó el Administrador inicial…". Este usuario es el Administrador.

### Registro y activación (RF-CA-01, 02, 14, 15, 16, 17)

1. Registrarse con un correo propio: `POST /api/auth/registro` → **201** "La cuenta se creó, pero todavía no está activa…" (RF-CA-01).
2. Iniciar sesión antes de activar: `POST /api/auth/login` con la contraseña correcta → **403** "La cuenta no está activa. Revisa tu correo para activarla." (RF-CA-15).
3. Terminal B: `dotnet run --project src/TrackingTiger -- enviar-correos` → llega el correo "Activa tu cuenta de Tracking Tiger" con el enlace.
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
2. Con sesión de Estándar, construir a mano `GET /api/usuarios` (también con `X-Rol: Administrador` o `?rol=Administrador`) → **403** "No tienes permiso para ejecutar esta operación. Se requiere el rol Administrador o un permiso aprobado y vigente." (RF-CA-06, RD-06).
3. Con sesión de Estándar, intentar cambiar el propio rol: `PUT /api/usuarios/{mi-id}/rol` → **403** (RF-CA-08).
4. Como Administrador:
   - `GET /api/usuarios` → lista con id, nombre, correo, rol y activo; nunca hashes ni tokens (RF-CA-21).
   - `PUT /api/usuarios/{id}/rol` con `{"rol":"Administrador"}` → **200**; surte efecto en la siguiente petición de ese usuario (RF-CA-08). Cambiar el propio rol → **400**.
   - Desactivar un usuario con sesión abierta: `POST /api/usuarios/{id}/desactivar` → **200**; su token deja de servir (**401**) y no puede iniciar sesión (**403** "Tu cuenta fue desactivada por un Administrador."). No puede reactivarse solo con un enlace de activación (RF-CA-20).
   - Intentar desactivarse a sí mismo → **400** "No puedes desactivar tu propia cuenta." (RF-CA-20).
   - `POST /api/usuarios/{id}/reactivar` → **200**; sus sesiones viejas siguen revocadas.

### Contraseñas (RF-CA-09, 10, 11, 12, 13, 22)

1. `POST /api/auth/recuperar` con un correo inexistente y con uno existente → **misma** respuesta 200 "Si el correo está registrado…" (RF-CA-09).
2. Terminal B: `dotnet run --project src/TrackingTiger -- enviar-correos` → llega el correo con el código (vence en 1 hora, un solo uso) (RF-CA-10).
3. `POST /api/auth/restablecer` con `{"codigo","nuevaContrasena"}` → **200**. Usar el mismo código otra vez → **400** "El código no es válido, ya fue usado o venció." y la contraseña no cambia (RF-CA-10, 11).
4. Login con la contraseña vieja → **401**; con la nueva → **200** (RF-CA-11).
5. Un token emitido antes del restablecimiento → **401** en `GET /api/auth/yo` (RF-CA-12).
6. Como Administrador: `POST /api/usuarios/{id}/forzar-restablecimiento` → **200**; la contraseña anterior deja de servir, sus sesiones se cierran y le llega por la cola el correo con el código (RF-CA-13).
7. Con sesión: `POST /api/auth/cambiar-contrasena` con una contraseña actual incorrecta → **400** "La contraseña actual no es correcta."; con la correcta → **200** y todas las sesiones se cierran (RF-CA-22, 14, 12).

### Correo por cola (RF-NOT-08, 09, 12, 13)

1. Apagar el acceso a SMTP (por ejemplo, terminal sin `call .env.cmd` o con `set SMTP_HOST=localhost` y `set SMTP_PORT=2599`) y registrar un usuario → **201** de inmediato; en DB Browser el correo queda en `CorreosEnCola` con `Estado = Pendiente` (RF-NOT-08).
2. Con las variables correctas: `dotnet run --project src/TrackingTiger -- enviar-correos` → el correo llega y pasa a `Enviado` (RF-NOT-09). Ejecutarlo otra vez → "No hay correos pendientes." y no se duplica nada (RF-NOT-12).
3. Las credenciales SMTP solo vienen de variables de entorno; no hay ninguna en el repositorio ni en el historial (RF-NOT-13, RD-10).

### Persistencia (RD-09)

Detener la API (`Ctrl+C`) y volver a ejecutar `dotnet run --project src/TrackingTiger`: los usuarios, las solicitudes de permiso y las tarjetas siguen ahí.

### Módulo de negocio: Tarjetas (RF-NEG-02 a 06, RF-NEG-09, RD-04, RD-07, RD-09)

Una tarjeta nace **Activa**. Si acumula 3 transacciones declinadas en 60 minutos, el sistema la pasa a **BloqueoSuave**. Un agente de fraude (usuario Estándar) registra la respuesta del cliente: si la reconoce vuelve a Activa; si no, pasa a **BloqueoTotal**, que solo se levanta con un permiso aprobado por un Administrador. **Cancelada** (pérdida o robo) es terminal.

- Estados en un solo lugar (RF-NEG-03): `src/TrackingTiger/Negocio/Tarjetas/EstadoTarjeta.cs`.
- Transiciones en un solo punto (RD-04), con las prohibidas explícitas (RF-NEG-04) y el estado terminal (RF-NEG-05): `src/TrackingTiger/Negocio/Tarjetas/MaquinaEstadosTarjeta.cs`. Tablas: `docs/maquina-de-estados.md`.
- Quién ejecuta cada operación (RF-NEG-06): `src/TrackingTiger/Core/ControlAcceso/CatalogoOperaciones.cs`. Sin sesión, todas responden **401**.
- RF-NEG-09: la tarjeta solo le pregunta a Gestión de permisos si el agente tiene un permiso vigente (`ServicioSolicitudesPermiso.TienePermisoVigenteAsync`); no usa los estados de la solicitud.

**Preparación** (no necesita SMTP). En una terminal CMD nueva:

1. `set DB_RUTA=%TEMP%\tt-tarjetas.db` (base de datos limpia para esta revisión).
2. `set ADMIN_CORREO_INICIAL=admin@ejemplo.com` · `set ADMIN_CLAVE_INICIAL=Clave12345` · `dotnet run --project src/TrackingTiger -- crear-admin` → usuario **1**, el Administrador.
3. `set ADMIN_CORREO_INICIAL=agente@ejemplo.com` · `dotnet run --project src/TrackingTiger -- crear-admin` → usuario **2**, el agente.
4. `dotnet run --project src/TrackingTiger` (dejar la API corriendo; las peticiones van en otra terminal o en `docs/pruebas.http`, sección «Tarjetas»).
5. Login de los dos: `POST /api/auth/login` con `{"correo":"admin@ejemplo.com","contrasena":"Clave12345"}` y con `agente@ejemplo.com`. Con el token del Administrador: `PUT /api/usuarios/2/rol` `{"rol":"Estandar"}` → **200**. El agente ahora es Estándar.

**Funcionalidades**, en orden (token del Administrador = *Admin*, token del agente = *Agente*):

1. **Emitir una tarjeta** — Agente: `POST /api/tarjetas` `{"titular":"Ana Pérez","ultimosCuatro":"4321","limiteCredito":100}` → **201** con `"id":1`, `"estado":"Activa"` y `"numeroEnmascarado":"**** 4321"`. Con `"ultimosCuatro":"12a4"` → **400** "ultimosCuatro debe tener exactamente 4 dígitos." (RD-07). Sin token → **401**.
2. **Registrar transacciones** — Agente: `POST /api/tarjetas/1/transacciones` `{"monto":40,"comercio":"Farmacia"}` → **200** "Transacción aprobada." y `"disponible":60`. Con `{"monto":500,"comercio":"Tienda en línea"}` tres veces → las dos primeras **200** "Transacción declinada: crédito disponible insuficiente."; la tercera agrega "La tarjeta pasó a BloqueoSuave por una ráfaga de declinaciones." Con la tarjeta bloqueada, una transacción se declina y el estado no cambia.
3. **Consultar la tarjeta** — Agente: `GET /api/tarjetas/1` → **200** con `"estado":"BloqueoSuave"`. `GET /api/tarjetas/999` → **404**.
4. **El cliente reconoce la actividad** — Admin: `POST /api/tarjetas/1/reconocer` → **403** "…Se requiere el rol Estándar." Agente: lo mismo → **200** `"estado":"Activa"`. (Para seguir, repetir las tres transacciones de 500 del paso 2: vuelve a BloqueoSuave.)
5. **El cliente no reconoce la actividad** — Agente: `POST /api/tarjetas/1/no-reconocer` `{"nota":""}` → **400** "Para pasar a BloqueoTotal se exige una nota." y no cambia. Con `{"nota":"El cliente no reconoce las compras."}` → **200** `"estado":"BloqueoTotal"`. Ahora `POST /api/tarjetas/1/reconocer` → **400** "Esta operación solo aplica a una tarjeta en BloqueoSuave…".
6. **Levantar el Bloqueo Total (con Gestión de permisos)** — Agente: `POST /api/tarjetas/1/levantar-bloqueo-total` `{"nota":"Validado por llamada."}` → **403** "Para levantar un Bloqueo Total necesitas el permiso levantar-bloqueo-total aprobado por un Administrador y vigente." Agente: `POST /api/permisos/solicitudes` `{"permiso":"levantar-bloqueo-total"}` → **201** Pendiente (anotar su `id`). Repetir el levantamiento → **403** otra vez (la solicitud no está aprobada). Admin: `POST /api/permisos/solicitudes/{id}/aprobar` `{"minutosVigencia":30}` → **200**. Agente: repetir el levantamiento → **200** `"estado":"Activa"`.
7. **Cancelar por pérdida o robo** — Agente: `POST /api/tarjetas/1/cancelar` `{"nota":"Robo reportado por el cliente."}` → **200** `"estado":"Cancelada"`. Estado terminal: `POST /api/tarjetas/1/reconocer`, `/no-reconocer` o `/levantar-bloqueo-total` → **400** y sigue Cancelada.
8. **Historial de transiciones** — Agente: `GET /api/tarjetas/1/historial` → **403** "…Se requiere el rol Administrador." Admin: lo mismo → **200** con cada transición: estado anterior y nuevo, `ejecutadoPor` ("Sistema" o el nombre del agente), nota y fecha.
9. **RD-09** — Detener la API (`Ctrl+C`) y ejecutar otra vez `dotnet run --project src/TrackingTiger` (misma terminal, mismo `DB_RUTA`): `GET /api/tarjetas/1` sigue en `Cancelada`.

### Gestión de permisos (RF-GP-01 a 07, RD-04, RD-06, RD-07, RD-09)

Un usuario Estándar pide permiso para ejecutar una operación concreta (por ahora `listar-usuarios`). Al aprobar, el Administrador elige cuántos minutos durará el permiso (`minutosVigencia`, de 1 a 43200 = 30 días); al vencer, el permiso deja de servir solo y el usuario puede pedirlo de nuevo.

- Transiciones en un solo lugar (RD-04): `src/TrackingTiger/Core/Permisos/MaquinaEstadosSolicitud.cs`. Permitidas: Pendiente → Aprobada (Administrador), Aprobada → Aplicada (el sistema, automática), Pendiente → Rechazada (Administrador, con motivo). Cualquier otra se rechaza y el estado no cambia.
- Estados: `src/TrackingTiger/Core/Permisos/EstadoSolicitud.cs`. La vigencia no es un estado de la solicitud: vive en el permiso otorgado (`PermisoOtorgado`, tabla `PermisosOtorgados`).
- Quién ejecuta cada operación: `src/TrackingTiger/Core/ControlAcceso/CatalogoOperaciones.cs`, verificado en el servidor (RD-06).

**Preparación** (no necesita SMTP: los dos usuarios se crean con `crear-admin`). En una terminal CMD nueva:

1. `set DB_RUTA=%TEMP%\tt-permisos.db` (base de datos limpia para esta revisión).
2. `set ADMIN_CORREO_INICIAL=admin@ejemplo.com` · `set ADMIN_CLAVE_INICIAL=Clave12345` · `dotnet run --project src/TrackingTiger -- crear-admin` → usuario **1**, el Administrador.
3. `set ADMIN_CORREO_INICIAL=ana@ejemplo.com` · `dotnet run --project src/TrackingTiger -- crear-admin` → usuario **2** (Ana).
4. `dotnet run --project src/TrackingTiger` (dejar la API corriendo; las peticiones van en otra terminal o en `docs/pruebas.http`).
5. Login de los dos: `POST /api/auth/login` con `{"correo":"admin@ejemplo.com","contrasena":"Clave12345"}` y con `ana@ejemplo.com`. Con el token del Administrador: `PUT /api/usuarios/2/rol` `{"rol":"Estandar"}` → **200**. Ana ahora es Estándar.

**Criterios**, en orden (token del Administrador = *Admin*, token de Ana = *Ana*):

1. Ana: `GET /api/usuarios` → **403** "…Se requiere el rol Administrador o un permiso aprobado y vigente." (todavía no tiene el permiso).
2. **RF-GP-01** — Ana: `POST /api/permisos/solicitudes` `{"permiso":"listar-usuarios"}` → **201** con `"estado":"Pendiente"` e `"id":1`. Repetirlo → **409** "Ya tienes una solicitud pendiente para ese permiso." Con `{"permiso":"cambiar-rol"}` → **400** "Ese permiso no se puede solicitar…" (RD-07).
3. **RF-GP-02** — Admin: `GET /api/permisos/solicitudes/pendientes` → **200** con la solicitud 1. Ana: el mismo endpoint → **403**; Ana: `GET /api/permisos/solicitudes/mias` → **200** solo con las suyas.
4. **RF-GP-03 y RF-GP-05** — Admin: `POST /api/permisos/solicitudes/1/aprobar` `{"minutosVigencia":2}` → **200** con `"estado":"Aplicada"`, `"aprobadorId":1`, `fechaResolucion` (cuándo) y `permisoVigenteHasta`. Sin `minutosVigencia` → **400**.
5. **RF-GP-05** — Ana: `GET /api/usuarios` → **200** con la lista, sin ningún paso manual. Esperar 2 minutos y repetir → **403**: el permiso venció solo. Mientras está vigente, pedirlo otra vez → **409** "Ya tienes ese permiso vigente hasta…".
6. **RF-GP-06** — Admin: aprobar otra vez la solicitud 1 → **400** "La solicitud ya está en Aplicada: no puede cambiar de estado."; `GET /api/permisos/solicitudes/mias` (Ana) muestra que sigue en `Aplicada`.
7. **RF-GP-04** — Ana crea otra solicitud (id 2). Admin: `POST /api/permisos/solicitudes/2/rechazar` `{"motivo":"   "}` → **400** "Para pasar a Rechazada se exige un motivo escrito." y sigue Pendiente. Con `{"motivo":"No lo necesita."}` → **200** `"estado":"Rechazada"`, `rechazadaPorId`, `motivoRechazo` y `fechaResolucion`.
8. **RF-GP-06** — Admin: `POST /api/permisos/solicitudes/2/aprobar` `{"minutosVigencia":5}` → **400** "La solicitud ya está en Rechazada: no puede cambiar de estado." (de Rechazada no se pasa a Aplicada).
9. **RF-GP-07** — Ana crea otra solicitud (id 3). Admin: `PUT /api/usuarios/2/rol` `{"rol":"Administrador"}`. Ana (ahora Administradora): `POST /api/permisos/solicitudes/3/aprobar` `{"minutosVigencia":5}` → **400** "No puedes aprobar ni rechazar tu propia solicitud."; lo mismo con `/rechazar`. La solicitud 3 sigue en `Pendiente`.
10. **RD-09** — Detener la API (`Ctrl+C`) y ejecutar otra vez `dotnet run --project src/TrackingTiger` (misma terminal, mismo `DB_RUTA`): `GET /api/permisos/solicitudes/pendientes` sigue mostrando la solicitud 3.

## Pruebas

Desde la raíz del repositorio, en un clon limpio (no necesita variables de entorno ni levantar la aplicación):

```
dotnet test
```

Ejecuta toda la suite de `tests/TrackingTiger.Pruebas` (xUnit). Las pruebas de Gestión de permisos están en `tests/TrackingTiger.Pruebas/Permisos/` y no levantan la aplicación (RD-12):

- `MaquinaEstadosSolicitudPruebas.cs` ejercita `MaquinaEstadosSolicitud` directamente, sin base de datos.
- `ServicioSolicitudesPermisoPruebas.cs` ejercita `ServicioSolicitudesPermiso` con una base SQLite en memoria y un reloj fijo; cada prueba tiene su propia base vacía.

Las pruebas del módulo de negocio están en `tests/TrackingTiger.Pruebas/Tarjetas/` y tampoco levantan la aplicación:

- `MaquinaEstadosTarjetaPruebas.cs` ejercita `MaquinaEstadosTarjeta` directamente, sin base de datos: una prueba por cada fila de las tablas de `docs/maquina-de-estados.md`, el estado terminal y las condiciones.
- `IntegracionTarjetasPermisosPruebas.cs` es la prueba de integración: usa `ServicioTarjetas` y `ServicioSolicitudesPermiso` **reales** (sin dobles) sobre una base SQLite en memoria. Verifica las dos caras: levantar un Bloqueo Total se rechaza mientras la solicitud no está aprobada (pendiente o rechazada) y se permite después de que un Administrador la aprueba.

Cada prueba crea sus propios datos, así que son independientes y pasan en cualquier orden; cada una marca sus fases `Preparar`, `Actuar` y `Verificar` y lleva un comentario con el requisito o la fila de la tabla que verifica.

**Máquina de estados de la Tarjeta** (filas de `docs/maquina-de-estados.md`):

| Fila | Prueba |
|---|---|
| Permitida 1 — Activa → BloqueoSuave (sistema) | `IntentarTransicion_ActivaABloqueoSuavePorElSistema_SePermiteYQuedaEnElHistorialSinUsuario` |
| Permitida 2 — BloqueoSuave → Activa | `IntentarTransicion_BloqueoSuaveAActivaPorUnAgente_SePermite` |
| Permitida 3 — BloqueoSuave → BloqueoTotal | `IntentarTransicion_BloqueoSuaveABloqueoTotalPorUnAgenteConNota_SePermite` |
| Permitida 4 — BloqueoTotal → Activa | `IntentarTransicion_BloqueoTotalAActivaPorUnAgenteConNota_SePermite` |
| Permitida 5 — Activa → Cancelada | `IntentarTransicion_ActivaACanceladaPorUnAgenteConNota_SePermite` |
| Permitida 6 — BloqueoSuave → Cancelada | `IntentarTransicion_BloqueoSuaveACanceladaPorUnAgenteConNota_SePermite` |
| Permitida 7 — BloqueoTotal → Cancelada | `IntentarTransicion_BloqueoTotalACanceladaPorUnAgenteConNota_SePermite` |
| Prohibida 1 — Activa → BloqueoTotal | `IntentarTransicion_ActivaABloqueoTotal_SeRechazaYElEstadoNoCambia` |
| Prohibida 2 — BloqueoTotal → BloqueoSuave | `IntentarTransicion_BloqueoTotalABloqueoSuave_SeRechazaYElEstadoNoCambia` |
| Prohibida 3 — Cancelada → Activa | `IntentarTransicion_CanceladaAActiva_SeRechazaYElEstadoNoCambia` |
| Estado terminal — de Cancelada no se sale | `IntentarTransicion_DesdeCancelada_NoSaleANingunEstado`, `EsTerminal_Cancelada_NingunaTransicionParteDeEse` |
| Condiciones | `IntentarTransicion_ActivaABloqueoSuavePorUnAgente_SeRechazaYElEstadoNoCambia`, `IntentarTransicion_BloqueoSuaveAActivaSinAgente_SeRechazaYElEstadoNoCambia`, `IntentarTransicion_BloqueoSuaveABloqueoTotalSinNota_SeRechazaYElEstadoNoCambia`, `IntentarTransicion_BloqueoTotalAActivaSinNota_SeRechazaYElEstadoNoCambia`, `IntentarTransicion_ConOrigenExigidoDistintoDelActual_SeRechazaYElEstadoNoCambia` |

**Integración con Gestión de permisos:** `LevantarBloqueoTotalAsync_ConLaSolicitudPendiente_SeRechazaYLaTarjetaSigueEnBloqueoTotal`, `LevantarBloqueoTotalAsync_ConLaSolicitudRechazada_SeRechazaYLaTarjetaSigueEnBloqueoTotal`, `LevantarBloqueoTotalAsync_DespuesDeQueUnAdministradorApruebaLaSolicitud_LaTarjetaVuelveAActiva`.

**Gestión de permisos:**

| Requisito | Pruebas |
|---|---|
| RF-GP-01 — la solicitud nace en Pendiente | `CrearAsync_EstandarPideUnPermiso_LaSolicitudNaceEnPendiente` |
| RF-GP-02 — un Estándar no ve las solicitudes de otros | `ListarDeUsuarioAsync_Estandar_NoVeLasSolicitudesDeOtrosUsuarios`, `ListarPendientesAsync_Administrador_VeLasPendientesDeTodosLosUsuarios` |
| RF-GP-03 — queda registrado quién aprobó y cuándo | `Aprobar_SolicitudPendiente_QuedaAprobadaConQuienAproboYCuando`, `AprobarAsync_Administrador_RegistraQuienAproboYCuando`, `AprobarAsync_UsuarioEstandar_SeRechazaYLaSolicitudSiguePendiente` |
| RF-GP-04 — sin motivo, el rechazo no se completa | `Rechazar_SinMotivoEscrito_SeRechazaYSigueEnPendiente`, `RechazarAsync_SinMotivo_SeRechazaYLaSolicitudSiguePendiente`, `Rechazar_SolicitudPendienteConMotivo_QuedaRechazadaConElMotivo`, `RechazarAsync_AdministradorConMotivo_QuedaRechazadaConElMotivo`, `RechazarAsync_UsuarioEstandar_SeRechazaYLaSolicitudSiguePendiente` |
| RF-GP-05 — tras aprobar llega a Aplicada y el permiso queda efectivo | `AprobarAsync_Pendiente_LlegaAAplicadaYElPermisoQuedaVigente`, `PermisoOtorgado_AlLlegarSuVencimiento_DejaDeEstarVigente` |
| RF-GP-06 — solo las transiciones de la tabla | `Permitidas_SoloContieneLasTransicionesDeLaTabla`, `Aprobar_SolicitudYaAplicada_SeRechazaYElEstadoNoCambia`, `AprobarAsync_DosVeces_LaSegundaSeRechazaYNadaCambia`, `Aplicar_SolicitudRechazada_SeRechazaYElEstadoNoCambia`, `Aplicar_SolicitudPendiente_SeRechazaYSigueEnPendiente`, `Rechazar_SolicitudAprobada_SeRechazaYElEstadoNoCambia`, `Rechazar_SolicitudAplicada_SeRechazaYElEstadoNoCambia`, `RechazarAsync_SolicitudAplicada_SeRechazaYSigueAplicada` |
| RF-GP-07 — nadie resuelve su propia solicitud | `Aprobar_PropiaSolicitud_SeRechazaYSigueEnPendiente`, `Rechazar_PropiaSolicitud_SeRechazaYSigueEnPendiente`, `AprobarAsync_AdministradorSobreSuPropiaSolicitud_SeRechazaYSiguePendiente`, `RechazarAsync_AdministradorSobreSuPropiaSolicitud_SeRechazaYSiguePendiente` |

## Defectos

### Práctica 3 (módulo de negocio)

Cada etiqueta `practica-3-defecto-N` planta un defecto sobre `practica-3` (solo código de producción); con él, `dotnet test` falla al menos en la prueba indicada.

1. `IntentarTransicion_ActivaABloqueoTotal_SeRechazaYElEstadoNoCambia`: una tarjeta Activa no puede pasar directo a BloqueoTotal; el intento se rechaza y sigue Activa.
2. `IntentarTransicion_DesdeCancelada_NoSaleANingunEstado` (también `EsTerminal_Cancelada_NingunaTransicionParteDeEse` e `IntentarTransicion_CanceladaAActiva_SeRechazaYElEstadoNoCambia`): de Cancelada no se sale hacia ningún estado.
3. `IntentarTransicion_ActivaABloqueoSuavePorUnAgente_SeRechazaYElEstadoNoCambia`: el Bloqueo Suave solo lo pone el sistema; si lo intenta un agente se rechaza y la tarjeta sigue Activa.
4. `LevantarBloqueoTotalAsync_ConLaSolicitudPendiente_SeRechazaYLaTarjetaSigueEnBloqueoTotal` (también `LevantarBloqueoTotalAsync_ConLaSolicitudRechazada_SeRechazaYLaTarjetaSigueEnBloqueoTotal`), prueba de integración: sin un permiso aprobado en Gestión de permisos, levantar el Bloqueo Total se rechaza y la tarjeta sigue en BloqueoTotal.

### Práctica 2 (Gestión de permisos)

Defectos de la variante B (Práctica 2). Cada etiqueta `practica-2-defecto-N` planta uno sobre `practica-2`; con él, `dotnet test` falla al menos en la prueba indicada.

1. **RF-GP-04** — `RechazarAsync_SinMotivo_SeRechazaYLaSolicitudSiguePendiente` (también `Rechazar_SinMotivoEscrito_SeRechazaYSigueEnPendiente`): un rechazo con motivo vacío o de solo espacios no se completa y la solicitud sigue Pendiente.
2. **RF-GP-06** — `Rechazar_SolicitudAprobada_SeRechazaYElEstadoNoCambia` (también `Permitidas_SoloContieneLasTransicionesDeLaTabla`): una solicitud Aprobada no puede pasar a Rechazada.
3. **RF-GP-03** — `AprobarAsync_UsuarioEstandar_SeRechazaYLaSolicitudSiguePendiente`: un usuario Estándar que intenta aprobar recibe un rechazo y no se otorga ningún permiso.
4. **RF-GP-07** — `AprobarAsync_AdministradorSobreSuPropiaSolicitud_SeRechazaYSiguePendiente` (también `AprobarAsync_Administrador_RegistraQuienAproboYCuando`): un Administrador no puede resolver su propia solicitud y sí puede resolver las de otros.
5. **RF-GP-06** — `RechazarAsync_SolicitudAplicada_SeRechazaYSigueAplicada` (también `Rechazar_SolicitudAplicada_SeRechazaYElEstadoNoCambia`): una solicitud ya Aplicada no puede rechazarse.
6. **RF-GP-03** — `AprobarAsync_Administrador_RegistraQuienAproboYCuando` (también `Aprobar_SolicitudPendiente_QuedaAprobadaConQuienAproboYCuando`): al aprobar queda registrado el id del Administrador que aprobó y la fecha de la aprobación.

## Estructura

```
TrackingTiger.sln         Solución: agrupa la aplicación y sus pruebas (dotnet build / dotnet test)
src/TrackingTiger/        La aplicación
  Core/                   Control de acceso, correo, persistencia y web (no depende de Negocio)
    ControlAcceso/        Usuarios, sesiones, roles, catálogo de operaciones, recuperación
    Correo/               Cola de correos y enviador SMTP
    Persistencia/         ContextoDatos (EF Core + SQLite)
    Web/                  Manejo global de errores (RD-08)
  Negocio/Tarjetas/       Tarjeta, transacciones, historial, su máquina de estados, servicio y endpoints
  Migrations/             Migraciones de EF Core (se aplican solas al iniciar)
tests/TrackingTiger.Pruebas/  Pruebas automatizadas con xUnit (RD-12)
docs/                     pruebas.http, maquina-de-estados.md, requisitos y bitácoras
```

## Notas

- `bin/`, `obj/`, `trackingtiger.db` y `.env.cmd` están ignorados por Git.
- Ningún mensaje al usuario expone trazas, rutas ni consultas (RD-08); los errores inesperados responden 500 con un mensaje genérico.
- Todas las fechas se guardan en UTC desde un único reloj (`IReloj`, RD-11).
