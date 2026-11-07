# Registro de pruebas y mantenimiento

Notas de las pruebas realizadas y de los cambios de mantenimiento durante el desarrollo. Sirven de base para el informe de pruebas y mantenimiento de la entrega final (sprint 6).

## 1. Pruebas unitarias (xUnit)

Proyecto `VuelvoSeguro.Tests`. Prueban `ViajeService` contra una base SQLite en memoria que se crea limpia en cada prueba.

**Última ejecución:** 07/11/2026 · 13 pruebas · 13 correctas · 0 fallidas.

| N.º | Prueba | Requerimiento | Resultado esperado | Resultado |
|---|---|---|---|---|
| U01 | Publicar crea un viaje Publicado hacia la casa del pasajero | RF05 | Estado Publicado, destino = dirección de la casa, sin conductor | ✅ |
| U02 | Publicar sin seguro vigente | RF03 | Se rechaza | ✅ |
| U03 | Publicar sin indicar el origen | RF05 | Se rechaza | ✅ |
| U04 | Publicar con un viaje activo | RF05 | Se rechaza ("Ya tenés un viaje activo") | ✅ |
| U05 | Disponibles muestra solo viajes sin conductor | RF07 | El viaje aceptado deja de listarse | ✅ |
| U06 | Aceptar asigna el conductor | RF07, RF08 | Estado Aceptado con ese conductor | ✅ |
| U07 | Aceptar un viaje ya aceptado | RF07 | Se rechaza y el conductor original no cambia (sin doble asignación) | ✅ |
| U08 | Aceptar con conductor Pendiente | RF04 | Se rechaza | ✅ |
| U09 | Aceptar con conductor Rechazado | RF04 | Se rechaza | ✅ |
| U10 | Aceptar con un viaje en curso | RF07 | Se rechaza | ✅ |
| U11 | Ciclo completo Publicado → Aceptado → En curso → Finalizado | RF08 | Termina Finalizado con fechas de inicio y fin; ya no hay viaje activo | ✅ |
| U12 | Finalizar sin haber iniciado | RF08 | Se rechaza y el estado sigue Aceptado | ✅ |
| U13 | Iniciar un viaje de otro conductor | RF08 | Se rechaza | ✅ |

## 2. Pruebas funcionales de punta a punta

Se ejecutó la aplicación y se recorrió cada flujo en un navegador automatizado (Playwright), en tamaño celular y escritorio.

| Fecha | Sprint | Caso | Resultado |
|---|---|---|---|
| 30/10/2026 | 2 | Registro de pasajero con auto, registro de conductor, inicio de sesión de cada rol | ✅ |
| 30/10/2026 | 2 | Pantallas en celular, tablet y escritorio sin desplazamiento horizontal | ✅ |
| 07/11/2026 | 3 | Conductor envía documentos con licencia vencida | ✅ Muestra "La licencia está vencida" |
| 07/11/2026 | 3 | Conductor envía licencia, DNI (imagen) y antecedentes (PDF) | ✅ Pasa a "Aprobación del administrador" |
| 07/11/2026 | 3 | Administrador ve los documentos del conductor y lo aprueba | ✅ El conductor pasa a Aprobado |
| 07/11/2026 | 3 | Un conductor intenta abrir un documento desde la URL del administrador | ✅ Acceso denegado (RNF04) |
| 07/11/2026 | 3 | Administrador suspende a un pasajero, el pasajero intenta ingresar, luego se lo reactiva | ✅ Ingreso rechazado y luego permitido |
| 07/11/2026 | 4 | Pasajero publica el viaje y ve "Buscando conductor"; al volver al inicio se lo redirige a su viaje | ✅ |
| 07/11/2026 | 4 | Conductor aprobado ve el viaje, lo acepta, confirma que recogió al pasajero y lo finaliza | ✅ |
| 07/11/2026 | 4 | El pasajero ve cada cambio: conductor asignado, viaje en curso, "¡Llegaste a casa!" | ✅ |
| 07/11/2026 | 4 | El administrador ve el viaje Finalizado en el listado de Viajes | ✅ |
| 07/11/2026 | 4 | Un conductor intenta ver la pantalla de viaje de un pasajero | ✅ Acceso denegado |

## 3. Mantenimiento

| Fecha | Tipo | Cambio | Motivo |
|---|---|---|---|
| 30/10/2026 | Adaptativo | Vistas rehechas según el prototipo de pantallas, en lugar de la plantilla por defecto de Bootstrap | Mantener la interfaz validada con el prototipo (Avance 2) |
| 30/10/2026 | Adaptativo | Interfaz responsive: navegación inferior en el celular y superior en tablet y escritorio | RNF06, la plataforma se usa desde cualquier dispositivo |
| 30/10/2026 | Correctivo | Mensajes de error de Identity traducidos al español | Los errores de contraseña y correo aparecían en inglés |
| 07/11/2026 | Perfectivo | Los documentos de los conductores se guardan fuera de `wwwroot` y solo los sirve el administrador | RNF04, privacidad según la Ley 25.326 |
| 07/11/2026 | Preventivo | La aceptación de un viaje es una actualización condicional en la base | Evitar que dos conductores acepten el mismo viaje a la vez (RF07) |
| 07/11/2026 | Perfectivo | Suspensión de cuentas y cierre de las sesiones abiertas del usuario suspendido | Completar RF11 |
| 07/11/2026 | Perfectivo | Las pantallas del viaje se actualizan solas cada 15 a 20 segundos | No hay notificaciones en tiempo real en el alcance actual |
| 07/11/2026 | Adaptativo | Se quita la tarifa (RF06) y el importe del historial (RF10) | El alcance del proyecto no maneja dinero |

## 4. Pendiente para la entrega final

- Sprint 5: cancelación con motivo (un viaje En curso no se puede cancelar) e historial de viajes de pasajero y conductor.
- Sprint 6: pruebas de rendimiento (RNF01), usabilidad (RNF02), HTTPS y seguridad (RNF03), compatibilidad de navegadores (RNF06) y términos y condiciones (RNF08).
