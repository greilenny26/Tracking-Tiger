# Criterios de aceptación: notificaciones, cola de correos y reportes

Escritos antes de delegar. El trabajo del agente se acepta solo si cada criterio se cumple.

## Decisiones previas

- **Administrador activo:** usuario con rol Administrador y cuenta activa.
- **Agente de fraude activo:** usuario con rol Estándar y cuenta activa.
- **Agente que trabajó con una tarjeta:** agente de fraude activo que ejecutó una acción sobre ella (queda en su
  historial: reconocer, no reconocer, levantar el bloqueo, cancelar) en las últimas 48 horas, límite incluido.
  Registrar transacciones no cuenta: simulan la red de pago.
- **Operaciones nuevas** (todas exigen sesión; sin ella responden 401):

| Operación | Quién |
|---|---|
| `GET /api/notificaciones` (filtro opcional `?leida=true` o `false`) | Con sesión, solo las propias |
| `POST /api/notificaciones/{id}/leer` | Con sesión, solo las propias |
| `GET /api/notificaciones/preferencias` | Con sesión, la propia |
| `PUT /api/notificaciones/preferencias` con `{"correo": true}` o `false` | Con sesión, la propia |
| `GET /api/correos` (filtro opcional `?estado=Pendiente`, `Enviado` o `Fallido`) | Administrador |
| `GET /api/reportes/solicitudes-permiso?desde=&hasta=` | Con sesión |
| `GET /api/reportes/tarjetas/riesgo-por-comercio?desde=&hasta=` | Administrador |

- **Máximo de intentos de envío de un correo:** 3.
- Los correos de activación y recuperación de contraseña no son notificaciones: se encolan siempre.

## Notificaciones

| N.º | RF | Dado | Cuando | Entonces |
|---|---|---|---|---|
| N-01 | RF-NOT-01 | Dos Administradores activos, uno desactivado y dos Estándar | un Estándar crea una solicitud de permiso | cada Administrador activo tiene una notificación nueva sin leer; el desactivado y los Estándar, ninguna |
| N-02 | RF-NOT-01 | Una solicitud que se rechaza al crearla (permiso no válido o ya pendiente) | se intenta crear | no se crea ninguna notificación ni correo |
| N-03 | RF-NOT-02 | Una solicitud Pendiente | un Administrador la aprueba | solo el solicitante recibe una notificación nueva cuyo mensaje dice que fue aprobada |
| N-04 | RF-NOT-02 | Una solicitud Pendiente | un Administrador la rechaza con un motivo | solo el solicitante recibe una notificación nueva cuyo mensaje dice que fue rechazada e incluye ese motivo |
| N-05 | RF-NOT-02 | Una solicitud ya resuelta | se intenta resolver otra vez | se rechaza y no se crea ninguna notificación |
| N-06 | RF-NOT-03 | Un agente que reconoció la actividad de una tarjeta hace menos de 48 horas, otro que nunca actuó sobre ella y un Administrador | la tercera declinación en 60 minutos la pasa a BloqueoSuave | solo el primer agente recibe una notificación, con el id de la tarjeta y sus últimos 4 dígitos |
| N-07 | RF-NOT-03 | La misma tarjeta ya en BloqueoSuave | se registra otra transacción | no se crea otra notificación |
| N-07a | RF-NOT-03 | Un agente cuya última acción sobre la tarjeta fue hace más de 48 horas | la tarjeta pasa a BloqueoSuave | ese agente no recibe notificación |
| N-07b | RF-NOT-03 | Una tarjeta sobre la que ningún agente actuó en las últimas 48 horas | pasa a BloqueoSuave | el cambio de estado se completa y no se crea ninguna notificación ni correo |
| N-07c | RF-NOT-03 | Un agente que actuó varias veces sobre la tarjeta en las últimas 48 horas | la tarjeta pasa a BloqueoSuave | recibe una sola notificación |
| N-08 | RF-NOT-04 | Un usuario con una notificación leída y otra sin leer | consulta `GET /api/notificaciones` | ve ambas, cada una con `leida` correcto; con `?leida=false` ve solo la no leída |
| N-09 | RF-NOT-04 | Dos usuarios con notificaciones | uno consulta las suyas | no aparece ninguna del otro, aunque sea Administrador |
| N-10 | RF-NOT-05 | Una notificación propia sin leer | su destinatario la marca | responde 200 y queda leída; marcarla otra vez responde 200 y sigue leída |
| N-11 | RF-NOT-05 | Una notificación de otro usuario | alguien (incluido un Administrador) intenta marcarla | responde 403 y sigue sin leer |
| N-12 | RF-NOT-05 | Un id que no existe | se intenta marcar | responde 404 |
| N-13 | RF-NOT-04, 05 | Ninguna sesión | se invoca cualquier operación de notificaciones | responde 401 |
| N-14 | RF-NOT-06 | Un destinatario con el correo activado (valor por defecto) | se genera una notificación | existe la notificación interna y exactamente un correo Pendiente en la cola dirigido a su dirección |
| N-15 | RF-NOT-07 | Un destinatario que desactivó el correo | se genera una notificación | existe la notificación interna y no se encola ningún correo para él |
| N-16 | RF-NOT-07 | Dos Administradores, uno con el correo desactivado | se crea una solicitud | ambos tienen la notificación interna; solo se encola el correo del que lo tiene activado |

## Cola de correos

| N.º | RF | Dado | Cuando | Entonces |
|---|---|---|---|---|
| C-01 | RF-NOT-08 | El servidor de correo apagado | se crea o se resuelve una solicitud | la operación responde éxito y el correo queda Pendiente con 0 intentos |
| C-02 | RF-NOT-09 | Un correo Pendiente | se ejecuta `enviar-correos` | queda Enviado con fecha de envío, sin repetir la operación que lo creó |
| C-03 | RF-NOT-10 | Un correo Pendiente y el servidor apagado | se ejecuta el procesador 1 vez, y luego 2 veces | tras la primera sigue Pendiente con 1 intento; tras la segunda, con 2 intentos; tras la tercera queda Fallido con 3 intentos y el último error |
| C-04 | RF-NOT-10 | Un correo Fallido | se ejecuta el procesador con el servidor encendido | no se envía y sus intentos siguen en 3 |
| C-05 | RF-NOT-10 | Faltan variables SMTP obligatorias | se ejecuta el procesador | informa el problema y no suma intentos |
| C-06 | RF-NOT-11 | Correos en los tres estados | un Administrador consulta `GET /api/correos` | ve de cada uno destinatario, asunto, estado, intentos, fecha de creación, fecha de envío y último error; `?estado=Fallido` devuelve solo los fallidos; un estado no válido responde 400 |
| C-07 | RF-NOT-11 | La misma cola | se consulta | la respuesta no incluye el cuerpo del correo (lleva enlaces y códigos) |
| C-08 | RF-NOT-11 | Un usuario Estándar | consulta `GET /api/correos` | responde 403 |
| C-09 | RF-NOT-12 | Un correo Pendiente y el servidor encendido | se ejecuta el procesador dos veces seguidas | se envía exactamente una vez |
| C-10 | RF-NOT-13 | El repositorio y su historial | se buscan credenciales SMTP | no aparece ninguna; solo se leen de variables de entorno |

## Reportes

Las fechas `desde` y `hasta` son opcionales, con formato `yyyy-MM-dd`, en UTC y ambas incluidas.

**Core:** solicitudes de permiso agrupadas por día de creación y estado, con el total de cada grupo.

**Negocio:** «¿En qué comercios se concentran las compras declinadas y cuánto dinero representan?»
Por comercio: transacciones, declinadas, porcentaje de declinación, monto declinado y monto aprobado
(en DOP), ordenado por monto declinado de mayor a menor.

| N.º | RF | Dado | Cuando | Entonces |
|---|---|---|---|---|
| R-01 | RF-REP-01, 03 | Tres solicitudes Pendientes y una Rechazada creadas el mismo día | un Administrador pide el reporte | recibe dos filas (totales 3 y 1), no cuatro registros |
| R-02 | RF-REP-02, 03 | En el comercio A: dos compras declinadas (100 y 200) y una aprobada (50) | un Administrador pide el reporte | A aparece en una sola fila: 3 transacciones, 2 declinadas, 66.67 %, 300 declinado, 50 aprobado |
| R-03 | RF-REP-02 | Dos comercios con montos declinados distintos | se pide el reporte | aparece primero el de mayor monto declinado |
| R-04 | RF-REP-04 | Solicitudes de dos usuarios Estándar | uno de ellos pide el reporte del Core | los totales cuentan solo las suyas, aunque agregue otros parámetros a la petición |
| R-05 | RF-REP-04 | Un usuario Estándar | pide el reporte de comercios | responde 403 |
| R-06 | RF-REP-04 | Ninguna sesión | se pide cualquier reporte | responde 401 |
| R-07 | RF-REP-05 | Datos en dos días distintos | se pide cada reporte con un rango por cada día | los resultados son distintos y cada uno incluye solo los datos de su rango |
| R-08 | RF-REP-05 | Un registro a las 23:59:59 UTC del día `hasta` | se pide el reporte | está incluido |
| R-09 | RF-REP-05 | Una fecha mal formada, o `desde` posterior a `hasta` | se pide un reporte | responde 400 con un mensaje claro |
| R-10 | RF-REP-01, 02 | Un rango sin datos | se pide un reporte | responde 200 con una lista vacía |

## Reglas que aplican a todo

- G-01: `dotnet test` pasa, incluidas las pruebas anteriores sin modificarlas, y hay pruebas nuevas para estos criterios (RD-12).
- G-02: el Core no referencia a `Negocio` (RD-03).
- G-03: la agregación la hace la base de datos o una función del proyecto, no la interfaz (RF-REP-03).
- G-04: el README tiene las secciones «Notificaciones» y «Reportes» que permiten provocar cada evento,
  ejecutar el procesador, provocar un fallo de envío y consultar ambos reportes por rango de fechas.
