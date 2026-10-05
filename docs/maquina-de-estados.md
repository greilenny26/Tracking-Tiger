# Máquina de estados: AlertaFraude

Módulo de negocio de Tracking Tiger: detección de fraude en tarjetas de débito y crédito.
La entidad central es **AlertaFraude**: una alerta sobre una transacción sospechosa que un analista revisa y resuelve.

## Dónde está en el código

| Qué | Dónde |
|---|---|
| Entidad central con su atributo de estado | `Negocio/Alertas/AlertaFraude.cs` (tabla `AlertasFraude`) |
| Estados, declarados en un solo lugar (RF-NEG-03) | `Negocio/Alertas/EstadoAlerta.cs` |
| Transiciones, resueltas en un solo punto (RD-04) | `Negocio/Alertas/MaquinaEstadosAlerta.cs` |

Ninguna otra clase cambia el estado: `AlertaFraude.AplicarEstado` es `internal` y solo lo invoca `MaquinaEstadosAlerta.IntentarTransicion`.
Agregar una transición nueva se hace agregando una línea en `MaquinaEstadosAlerta.Permitidas`.

## Estados

| Estado | Significado | ¿Terminal? |
|---|---|---|
| Detectada | La detección marcó la transacción como sospechosa. Toda alerta nace aquí. | No |
| EnRevision | Un analista la está revisando. | No |
| Confirmada | Se confirmó que la transacción es fraude. | **Sí** (RF-NEG-05) |
| Descartada | Se descartó: la transacción era legítima. | **Sí** (RF-NEG-05) |

## Transiciones

| Desde | Hacia | Quién la ejecuta | Condición |
|---|---|---|---|
| Detectada | EnRevision | Estándar o Administrador | La alerta está en Detectada. |
| EnRevision | Confirmada | Administrador | La alerta está en EnRevision y se indica una observación. |
| EnRevision | Descartada | Administrador | La alerta está en EnRevision y se indica una observación. |
| Detectada | Confirmada | — | **Prohibida explícitamente** (RF-NEG-04): una alerta debe revisarse antes de confirmarse. |
| Descartada | Confirmada | — | **Prohibida explícitamente** (RF-NEG-04): una alerta descartada no puede confirmarse después. |
| Cualquier otra combinación | — | — | Prohibida. El sistema la rechaza y el estado no cambia. |

Desde los estados terminales (Confirmada y Descartada) no parte ninguna transición.

## Comportamiento ante una transición no permitida

`MaquinaEstadosAlerta.IntentarTransicion` devuelve un `ResultadoTransicion` con `Exito = false` y un mensaje en español, y **no modifica** la alerta. Al llegar a un estado terminal se registra la fecha de resolución en UTC.

## Pendiente (semana 8)

Pruebas automatizadas de la máquina de estados (suite propia, separada de la de Gestión de permisos) y endpoints del módulo, protegidos por Control de acceso (RF-NEG-06).
