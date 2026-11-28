# Criterios de aceptación: auditoría

Escritos antes de delegar. El trabajo del agente se acepta solo si cada criterio se cumple.

## Decisiones previas

- **Entidad `RegistroAuditoria`:** usuario que actuó, acción, entidad afectada, identificador de la entidad,
  fecha y hora (UTC, del reloj único `IReloj`), valor anterior y valor nuevo. Ningún campo queda vacío (RF-AUD-01).
- **Solo inserción:** la aplicación no tiene ninguna operación para editar ni borrar registros (RF-AUD-06).
- **Misma operación:** el registro se guarda junto con el cambio que audita. Si la operación se rechaza,
  no se guarda ningún registro; si el registro no se puede guardar, el cambio tampoco se guarda.
- **Nunca se guardan** contraseñas, hashes, códigos de recuperación ni tokens (RF-AUD-05).
- **Qué se audita:**

| Acción | Entidad | Usuario que actuó | Valor anterior | Valor nuevo |
|---|---|---|---|---|
| `AprobarSolicitud` | `SolicitudPermiso` | El Administrador | `Pendiente` | `Aplicada` |
| `RechazarSolicitud` | `SolicitudPermiso` | El Administrador | `Pendiente` | `Rechazada: <motivo>` |
| `SubirDocumento` | `Documento` | Quien lo subió | `No existía` | `Activo: <nombre>` |
| `EliminarDocumento` | `Documento` | Quien lo eliminó | `Activo` | `Eliminado` |
| `CambiarRol` | `Usuario` | El Administrador | Rol anterior | Rol nuevo |
| `RestablecerContrasena` | `Usuario` | El propio usuario | `Iniciado por: Usuario` | `Contraseña reemplazada` |
| `ForzarRestablecimiento` | `Usuario` | El Administrador | `Iniciado por: Administrador` | `Restablecimiento obligatorio` |
| `CambiarContrasenaPropia` | `Usuario` | El propio usuario | `Iniciado por: Usuario` | `Contraseña reemplazada` |

- **Operación nueva** (exige sesión; sin ella responde 401):

| Operación | Quién |
|---|---|
| `GET /api/auditoria?usuarioId=&entidad=&desde=&hasta=` (todos los filtros son opcionales) | Administrador |

  `usuarioId` filtra por el usuario que actuó. `entidad` admite `SolicitudPermiso`, `Documento` o `Usuario`.
  `desde` y `hasta` usan el formato `yyyy-MM-dd`, en UTC y ambas incluidas. El resultado va del más reciente al más antiguo.

## Registro

| N.º | RF | Dado | Cuando | Entonces |
|---|---|---|---|---|
| A-01 | RF-AUD-01 | Cualquier acción de la tabla anterior | se completa | su registro tiene los siete datos llenos y la fecha es la del reloj del sistema |
| A-02 | RF-AUD-02 | Una solicitud Pendiente | un Administrador la aprueba | hay un registro `AprobarSolicitud` con su id, el id de la solicitud, `Pendiente` → `Aplicada` |
| A-03 | RF-AUD-02 | Una solicitud Pendiente | un Administrador la rechaza con un motivo | hay un registro `RechazarSolicitud` cuyo valor nuevo incluye el motivo |
| A-04 | RF-AUD-02 | Una solicitud ya resuelta, o un rechazo sin motivo | se intenta resolver | se rechaza y no se crea ningún registro |
| A-05 | RF-AUD-03, RF-DOC-06 | Un usuario con sesión | sube un documento | hay un registro `SubirDocumento` con el id del documento y su nombre |
| A-06 | RF-AUD-03, RF-DOC-06 | Un documento propio | su dueño lo elimina | hay un registro `EliminarDocumento`, `Activo` → `Eliminado` |
| A-07 | RF-AUD-03 | Un documento ajeno, o una subida rechazada | se intenta la operación | se rechaza y no se crea ningún registro |
| A-08 | RF-AUD-04 | Un usuario Estándar | un Administrador lo cambia a Administrador | hay un registro `CambiarRol` con `Estándar` → `Administrador` |
| A-09 | RF-AUD-04 | Un cambio de rol rechazado (Estándar que lo intenta, o el Administrador sobre sí mismo) | se intenta | no se crea ningún registro |
| A-10 | RF-AUD-05 | Un código de recuperación válido | el usuario restablece su contraseña | hay un registro `RestablecerContrasena` con el id del usuario como quien actuó e `Iniciado por: Usuario` |
| A-11 | RF-AUD-05 | Un usuario | un Administrador fuerza su restablecimiento | hay un registro `ForzarRestablecimiento` con el id del Administrador e `Iniciado por: Administrador` |
| A-12 | RF-AUD-05 | Un código usado o vencido | se intenta restablecer | se rechaza y no se crea ningún registro |
| A-13 | RF-AUD-05 | Los registros de A-10 y A-11 | se leen directamente de la base de datos | ningún campo contiene la contraseña, su hash ni el código |

## Inmutabilidad

| N.º | RF | Dado | Cuando | Entonces |
|---|---|---|---|---|
| A-14 | RF-AUD-06 | El código de la aplicación | se buscan operaciones sobre la auditoría | solo existen insertar y consultar; no hay ninguna ruta `PUT`, `PATCH` ni `DELETE` |
| A-15 | RF-AUD-06 | Un registro existente | se invoca `PUT` o `DELETE` sobre `/api/auditoria/{id}` | responde 404 o 405 y el registro no cambia |

## Consulta

| N.º | RF | Dado | Cuando | Entonces |
|---|---|---|---|---|
| A-16 | RF-AUD-07 | Registros de dos usuarios | un Administrador filtra por `usuarioId` de uno | recibe solo los registros de ese usuario |
| A-17 | RF-AUD-07 | Registros de las tres entidades | un Administrador filtra por `entidad=Documento` | recibe solo los de documentos |
| A-18 | RF-AUD-07 | Registros en dos días distintos | un Administrador filtra con un rango por cada día | cada respuesta incluye solo los registros de su día; uno a las 23:59:59 UTC del día `hasta` está incluido |
| A-19 | RF-AUD-07 | Los tres filtros juntos | un Administrador consulta | se aplican todos a la vez |
| A-20 | RF-AUD-07 | Un usuario Estándar | consulta `GET /api/auditoria` | responde 403, aunque pida solo sus propios registros |
| A-21 | RF-AUD-07 | Ninguna sesión | consulta `GET /api/auditoria` | responde 401 |
| A-22 | RF-AUD-07 | Una fecha mal formada, `desde` posterior a `hasta`, una entidad no válida o un `usuarioId` que no es número | se consulta | responde 400 con un mensaje claro |
| A-23 | RF-AUD-07 | Un filtro sin resultados | se consulta | responde 200 con una lista vacía |

## Reglas que aplican a todo

- G-01: `dotnet test` pasa, incluidas las pruebas anteriores sin modificarlas, y hay pruebas nuevas para estos criterios (RD-12).
- G-02: el Core no referencia a `Negocio` (RD-03).
- G-03: la exigencia de rol de la consulta se declara en `CatalogoOperaciones`, como el resto (RF-CA-05).
- G-04: el README tiene la sección «Auditoría», que explica cómo provocar cada acción auditada y consultar los registros con cada filtro.
