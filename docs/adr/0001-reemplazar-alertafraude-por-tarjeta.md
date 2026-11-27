# ADR-0001: Reemplazar `AlertaFraude` por `Tarjeta` como entidad central del negocio

- **Estado:** Aceptada
- **Fecha:** 2026-11-01 (commit `df324a5`, migración `20261101190612_ReemplazarAlertaFraudePorTarjeta`)
- **Requisitos que toca:** RF-NEG-01, RF-NEG-02, RF-NEG-03, RF-NEG-04, RF-NEG-05, RF-NEG-09, RD-03, RD-04
- **Entrega en que se aplicó:** Práctica 3 (`practica-3`)

## Contexto

El dominio elegido es un sistema antifraude para tarjetas de crédito. En las prácticas 1 y 2 la entidad central
del módulo de negocio era `AlertaFraude`: una alerta con número enmascarado, monto, comercio, nivel de riesgo y
una máquina de 4 estados (`Detectada → EnRevision → Confirmada | Descartada`).

Al llegar a la Práctica 3 (módulo completo con al menos 5 funcionalidades, máquina de estados propia, integración
con Gestión de permisos y suite de pruebas) se vio que esa entidad no alcanzaba:

1. **Una sola entidad con estado.** RF-NEG-01 pide al menos cuatro entidades relacionadas en el modelo de datos, y
   las relaciones de `AlertaFraude` eran débiles (los datos de la tarjeta y la transacción iban pegados dentro de
   la alerta).
2. **No había ciclo de vida real.** Los dos estados finales (`Confirmada`, `Descartada`) eran un veredicto, no el
   estado de algo que se opera. No había un bloqueo que se levante con un permiso, que es lo que conecta con
   Gestión de permisos (RF-NEG-09).
3. **Pocas funcionalidades separables.** Crear, revisar, confirmar y descartar eran variaciones de lo mismo; era
   difícil llegar a 5 que se probaran por separado (RF-NEG-02).

## Decisión

Se elimina `AlertaFraude` y se modela el negocio alrededor de la **tarjeta**, que es lo que realmente se protege:

- **Entidades** (`Negocio/Tarjetas/`): `Tarjeta` (solo guarda `UltimosCuatro`, nunca el número completo),
  `Transaccion` y `HistorialEstadoTarjeta` (con `UsuarioId = null` cuando lo hizo el sistema). Se relacionan con
  `Usuario` del Core solo por id, sin que el Core conozca el negocio (RD-03).
- **Estados:** `Activa`, `BloqueoSuave`, `BloqueoTotal` y `Cancelada` (terminal), declarados en `EstadoTarjeta.cs`.
- **Máquina de estados:** `MaquinaEstadosTarjeta.cs` es el único punto de transiciones (RD-04), con 7 permitidas y
  3 prohibidas explícitas (`Activa→BloqueoTotal`, `BloqueoTotal→BloqueoSuave`, `Cancelada→Activa`).
- **El sistema detecta el fraude:** 3 transacciones declinadas en 60 minutos pasan la tarjeta a `BloqueoSuave`
  (transición que solo ejecuta el sistema).
- **Un agente (usuario Estándar) resuelve el caso** con la respuesta del cliente. Levantar un `BloqueoTotal` exige
  un permiso `levantar-bloqueo-total` aprobado y vigente en Gestión de permisos, consultado con
  `ServicioSolicitudesPermiso.TienePermisoVigenteAsync` (el negocio no usa los estados de la solicitud).
- **8 funcionalidades** en `ServicioTarjetas`: emitir, registrar transacción, consultar, reconocer, no reconocer,
  levantar bloqueo total, cancelar e historial.
- Se agrega una migración que elimina la tabla de alertas y crea las de tarjetas. No se migran datos: el módulo
  no estaba en uso.

## Alternativas consideradas

| Alternativa | Por qué se descartó |
|---|---|
| Dejar `AlertaFraude` y agregarle más estados | Seguía sin haber detección real ni relación con la tarjeta; los estados extra serían decorativos. |
| Mantener `AlertaFraude` **y** agregar `Tarjeta` | Dos máquinas de estados en el mismo módulo y dos fuentes de verdad; viola el espíritu de RD-04. |
| Cambiar a otro dominio del catálogo | Perdía lo ya hecho en el Core y el análisis del antifraude; el dominio era válido, el modelo no. |

## Consecuencias

**Positivas**
- Modelo con tres entidades propias más `Usuario` relacionadas; ciclo de vida con una transición exclusiva del sistema.
- Integración natural con Gestión de permisos y con una prueba de dos caras (antes y después de aprobar).
- Se puede notificar, reportar y auditar con eventos del dominio claros (bloqueo, cancelación, levantamiento).
  Esto da base a RF-NOT-03, RF-NEG-08 (reportes) y la auditoría de la pieza 6.

**Negativas / costos**
- `docs/bitacora/plan-practica-1.md` y `docs/bitacora/practica-1.md` siguen hablando de `AlertaFraude`: son
  históricos y no se reescriben; este ADR explica el cambio.
- Se perdió la clasificación por nivel de riesgo de la alerta. Si se quisiera de nuevo, iría como regla del
  servicio sobre `Transaccion`, no como entidad.
- Dos ideas del informe de diseño original (nivel de riesgo, lista de comercios sospechosos) quedan como trabajo
  futuro.

## Cómo defenderlo en la evaluación oral

> "Cambié `AlertaFraude` por `Tarjeta` porque la alerta era un registro suelto, sin ciclo de vida ni relaciones
> con otras entidades. Con la tarjeta, el sistema detecta la ráfaga y la bloquea, el agente la resuelve y levantar un
> bloqueo total pasa por Gestión de permisos. Es una sola máquina de estados, en un solo archivo, y el Core no sabe
> que existe."

## Referencias

- `docs/maquina-de-estados.md` (tablas de transiciones)
- `README.md`, sección «Módulo de negocio: Tarjetas»
- Commit `df324a5` «Reemplaza AlertaFraude por la entidad Tarjeta (RF-NEG-03)»
