# Máquina de estados: Tarjeta

Módulo de negocio de Tracking Tiger: sistema antifraude para tarjetas de crédito.
La entidad central es la **Tarjeta**: cuando su actividad parece sospechosa se bloquea, y un agente de fraude
(usuario Estándar) resuelve el caso con la respuesta del cliente.

## Dónde está en el código

| Qué | Dónde |
|---|---|
| Entidad central con su atributo de estado | `src/TrackingTiger/Negocio/Tarjetas/Tarjeta.cs` (tabla `Tarjetas`) |
| Estados, declarados en un solo lugar (RF-NEG-03) | `src/TrackingTiger/Negocio/Tarjetas/EstadoTarjeta.cs` |
| Transiciones, resueltas en un solo punto (RD-04) | `src/TrackingTiger/Negocio/Tarjetas/MaquinaEstadosTarjeta.cs` |
| Historial de cada transición | `src/TrackingTiger/Negocio/Tarjetas/HistorialEstadoTarjeta.cs` (tabla `HistorialEstadosTarjeta`) |

Ninguna otra clase cambia el estado: `Tarjeta.AplicarEstado` es `internal` y solo lo invoca
`MaquinaEstadosTarjeta.IntentarTransicion`. Agregar una transición se hace agregando una línea en
`MaquinaEstadosTarjeta.Permitidas`.

## Estados

| Estado | Significado | ¿Terminal? |
|---|---|---|
| Activa | Opera con normalidad. Toda tarjeta nace aquí. | No |
| BloqueoSuave | Bloqueo temporal por actividad sospechosa; se levanta si el cliente reconoce la actividad. | No |
| BloqueoTotal | El cliente no reconoció la actividad; solo un agente con permiso aprobado lo levanta. | No |
| Cancelada | Pérdida o robo. La tarjeta no se reactiva. | **Sí** (RF-NEG-05) |

## Transiciones permitidas

| # | Desde | Hacia | Quién la ejecuta | Condición |
|---|---|---|---|---|
| 1 | Activa | BloqueoSuave | Sistema | Solo el sistema, al detectar una ráfaga (3 declinaciones en 60 minutos). Ningún usuario la ejecuta a mano. |
| 2 | BloqueoSuave | Activa | Agente (Estándar) | El cliente reconoció la actividad. |
| 3 | BloqueoSuave | BloqueoTotal | Agente (Estándar) | El cliente no reconoce la actividad; se exige una nota. |
| 4 | BloqueoTotal | Activa | Agente (Estándar) con permiso aprobado | Permiso `levantar-bloqueo-total` aprobado y vigente en Gestión de permisos; se exige una nota. |
| 5 | Activa | Cancelada | Agente (Estándar) | Pérdida o robo; se exige una nota. |
| 6 | BloqueoSuave | Cancelada | Agente (Estándar) | Pérdida o robo; se exige una nota. |
| 7 | BloqueoTotal | Cancelada | Agente (Estándar) | Pérdida o robo; se exige una nota. |

## Transiciones prohibidas

| # | Desde | Hacia | Motivo |
|---|---|---|---|
| 1 | Activa | BloqueoTotal | Toda tarjeta pasa primero por Bloqueo Suave. |
| 2 | BloqueoTotal | BloqueoSuave | Un Bloqueo Total no se rebaja a Bloqueo Suave. |
| 3 | Cancelada | Activa | Una tarjeta cancelada por pérdida o robo nunca vuelve a Activa. |

Prohibidas explícitamente (RF-NEG-04). Cualquier otra combinación que no esté en la tabla de permitidas
también se rechaza. Desde Cancelada no parte ninguna transición.

## Comportamiento ante una transición no permitida

`MaquinaEstadosTarjeta.IntentarTransicion` devuelve un `ResultadoTransicionTarjeta` con `Exito = false` y un
mensaje en español, y **no modifica** la tarjeta ni registra historial (RD-07).

## Independencia de Gestión de permisos (RF-NEG-09)

La tarjeta tiene su propia máquina de estados. La transición 4 solo pregunta a Gestión de permisos si el agente
tiene un permiso vigente (`ServicioSolicitudesPermiso.TienePermisoVigenteAsync`); no usa los estados de la
solicitud, así que cambiarlos no obliga a cambiar los de la tarjeta.

## Pendiente (semanas siguientes)

Correo al cliente con enlace para reconocer o no la actividad, plazo de respuesta, scoring y periodo de
aprendizaje (RF-NEG-07 y siguientes).
