# Informe de pruebas y mantenimiento

**Proyecto:** VuelvoSeguro · **Materia:** Taller de Proyectos II · **Sprint 6:** Pruebas, implementación y evaluación
**Fecha:** 19/11/2026 · **Versión probada:** rama `sprint-5` (sprints 5 y 6 sobre la base del sprint 4)

Este informe reúne todas las pruebas hechas durante el desarrollo (sprints 2 a 6) y los cambios de mantenimiento. El detalle día por día de los sprints anteriores está en [registro-pruebas-y-mantenimiento.md](registro-pruebas-y-mantenimiento.md).

## 1. Resumen

| Tipo de prueba | Herramienta | Casos | Correctos | Fallidos |
|---|---|---|---|---|
| Unitarias y de integración con la base | xUnit + SQLite en memoria | 43 | 43 | 0 |
| Funcionales de punta a punta (sprints 2 a 4) | Playwright, Chrome | 12 | 12 | 0 |
| Funcionales de punta a punta (sprints 5 y 6) | Playwright, Chrome y WebKit | 37 | 37 | 0 |
| Seguridad (encabezados, HTTPS, CSRF, accesos) | curl + Playwright + xUnit | 10 | 10 | 0 |
| Rendimiento | xUnit (1000 viajes) + tiempos medidos en el navegador | 9 | 9 | 0 |

Todos los requerimientos funcionales del alcance (RF01 a RF05 y RF07 a RF11) tienen al menos una prueba automática. RF06 (tarifa) queda fuera del alcance porque el proyecto no maneja dinero.

## 2. Entorno de prueba

- Windows 11 Pro, .NET 9, SQL Server Express (base `VuelvoSeguroDb`), aplicación en el perfil `https` (`https://localhost:7090`).
- Navegadores: Google Chrome (motor Blink, el de Chrome, Edge y Opera) y WebKit 18.2 (motor de Safari en iPhone y Mac), con Playwright 1.49.
- Tamaños: celular (390 × 844, táctil) y escritorio (1280 × 800).
- Script de las pruebas de punta a punta: [`pruebas-e2e/e2e.js`](../pruebas-e2e/e2e.js). Capturas en [`docs/capturas`](capturas).

## 3. Pruebas unitarias (xUnit)

Proyecto `VuelvoSeguro.Tests`. Cada prueba usa una base SQLite en memoria creada de cero, así las reglas se prueban contra una base real (con claves foráneas y actualizaciones condicionales) y sin depender de SQL Server.

**Última ejecución:** 19/11/2026 · `dotnet test` · 43 pruebas · 43 correctas · 0 fallidas · 1 s.

### 3.1 Viajes: publicar, aceptar y estados (sprint 4)

U01 a U13 del registro: publicar hacia la casa del pasajero, rechazos sin seguro / sin origen / con viaje activo, listado de disponibles, aceptación sin doble asignación, conductores no aprobados y ciclo completo de estados. 13 de 13 correctas.

### 3.2 Cancelación con motivo (RF09, sprint 5)

| N.º | Prueba | Resultado esperado | Resultado |
|---|---|---|---|
| U14 | El pasajero cancela un viaje Publicado | Queda Cancelado con motivo, fecha y quién canceló | ✅ |
| U15 | El pasajero cancela un viaje Aceptado | Se cancela y el conductor queda libre para otro viaje | ✅ |
| U16 | El conductor cancela un viaje Aceptado | Se cancela, queda registrado que lo hizo el conductor y sigue en su historial | ✅ |
| U17 | Cancelar un viaje En curso (pasajero y conductor) | Se rechaza: "Un viaje en curso no se puede cancelar." | ✅ |
| U18 | Cancelar un viaje Finalizado | Se rechaza y sigue Finalizado | ✅ |
| U19 | Cancelar dos veces | La segunda se rechaza y el motivo original no cambia | ✅ |
| U20-22 | Cancelar sin motivo (nulo, vacío, solo espacios) | Se rechaza y el viaje sigue Publicado | ✅ |
| U23 | Motivo de más de 200 caracteres | Se rechaza | ✅ |
| U24 | Cancelar el viaje de otra persona | Se rechaza (ni otro pasajero ni otro conductor) | ✅ |
| U25 | Después de cancelar, publicar otro viaje | Se permite | ✅ |
| U26 | Un conductor intenta aceptar un viaje cancelado | Se rechaza y no aparece en disponibles | ✅ |
| U27 | El conductor intenta iniciar un viaje que el pasajero canceló | Se rechaza | ✅ |

### 3.3 Historial de viajes (RF10, sprint 5)

| N.º | Prueba | Resultado esperado | Resultado |
|---|---|---|---|
| U28 | Usuario sin viajes | Historial vacío | ✅ |
| U29 | Viajes finalizados, cancelados y uno activo | Muestra los finalizados y cancelados, no el activo | ✅ |
| U30 | Historial del conductor | Solo sus viajes, con los datos del pasajero cargados | ✅ |
| U31 | Dos pasajeros | No se mezclan los viajes | ✅ |
| U32 | Orden | Del más nuevo al más viejo | ✅ |

### 3.4 Validaciones y seguridad (sprint 6)

| N.º | Prueba | Requerimiento | Resultado |
|---|---|---|---|
| U33 | Registro sin aceptar los términos | RNF08 | ✅ Inválido, error en `AceptaTerminos` |
| U34 | Registro aceptando los términos | RNF08 | ✅ Válido |
| U35 | Encabezados de seguridad en la respuesta | RNF03 | ✅ nosniff, DENY, CSP `frame-ancestors 'none'`, Referrer-Policy |
| U36-41 | Armado del motivo: motivo de la lista, "Otro" con texto, y casos vacíos | RF09 | ✅ |

### 3.5 Rendimiento (RNF01)

| N.º | Prueba | Resultado esperado | Resultado |
|---|---|---|---|
| U42 | Viajes disponibles con 1000 viajes cargados | Devuelve los 250 publicados en menos de 1 s | ✅ |
| U43 | Historial de pasajero y de conductor con 1000 viajes | Devuelve 750 y 500 viajes en menos de 1 s cada uno | ✅ |

## 4. Pruebas funcionales de punta a punta (sprints 5 y 6)

Se ejecutó la aplicación y el script recorrió los flujos como lo haría una persona: el pasajero en un celular y el conductor en una computadora. Se repitió completo en Chrome y en WebKit con usuarios nuevos.

| Caso | Chrome | WebKit |
|---|---|---|
| La página de términos y condiciones se ve sin iniciar sesión y sin desplazamiento horizontal | ✅ | ✅ |
| Registrarse sin marcar "Acepto los términos y condiciones" no avanza | ✅ | ✅ |
| Registrarse marcándolo entra al inicio del pasajero (se guarda la fecha de aceptación) | ✅ | ✅ |
| La pantalla de cancelación entra en el celular sin desplazamiento horizontal | ✅ | ✅ |
| Cancelar sin elegir un motivo no se envía | ✅ | ✅ |
| El pasajero cancela un viaje publicado con "Otro motivo" y ve el motivo | ✅ | ✅ |
| El conductor acepta un viaje y lo cancela con un motivo de la lista | ✅ | ✅ |
| El pasajero ve "Tu conductor canceló el viaje" con el motivo y puede pedir otro | ✅ | ✅ |
| Con el viaje En curso no aparece el botón de cancelar (ni al pasajero ni al conductor) | ✅ | ✅ |
| Entrar a mano a la URL de cancelar un viaje en curso lo rechaza con el mensaje | ✅ | ✅ |
| El historial del pasajero muestra sus 3 viajes (finalizado, cancelado por el conductor, cancelado por él) | ✅ | ✅ |
| El historial del pasajero entra en el celular sin desplazamiento horizontal | ✅ | ✅ |
| El historial del conductor muestra sus 2 viajes y está en su barra de navegación | ✅ | ✅ |
| Un pasajero que abre el historial del conductor recibe "Acceso denegado" | ✅ | ✅ |
| El administrador ve en Viajes quién canceló y el motivo | ✅ | |

**Total:** 37 casos, 37 correctos.

## 5. Pruebas de requerimientos no funcionales

### 5.1 Rendimiento (RNF01)

Además de las pruebas U42 y U43, se midió en el navegador el tiempo de carga completo de cada pantalla (desde que se pide hasta el evento `load`), con la aplicación y la base en la misma computadora. Cada pantalla se cargó entre 10 y 12 veces entre los dos navegadores.

| Pantalla | Promedio | Máximo |
|---|---|---|
| Inicio de sesión | 65 ms | 79 ms |
| Inicio del pasajero ("Volver a casa") | 78 ms | 106 ms |
| Historial del pasajero | 78 ms | 128 ms |
| Viajes disponibles (conductor) | 82 ms | 99 ms |
| Historial del conductor | 73 ms | 91 ms |
| Términos y condiciones | 60 ms | 73 ms |
| Viajes (administrador) | 116 ms | 116 ms |

Todas las pantallas cargan muy por debajo de 2 segundos. Son tiempos en red local: en un servidor real hay que sumar la latencia de la red, pero el margen es amplio. Las consultas usan índices en las claves foráneas (pasajero, conductor, estado) que crean las migraciones.

### 5.2 Usabilidad (RNF02)

Se hizo una revisión heurística (heurísticas de Nielsen) de los flujos, pensando en una persona que tomó alcohol y usa el celular:

| Criterio | Cómo se cumple | Resultado |
|---|---|---|
| Pocos pasos para la tarea principal | Pedir el viaje lleva 1 clic más escribir dónde está; el destino es siempre la casa cargada en el registro | ✅ |
| Visibilidad del estado | El viaje muestra en qué paso está (buscando conductor, en camino, en curso, llegada) y se actualiza solo | ✅ |
| Prevención de errores | Cancelar pide confirmar con un motivo y tiene "Volver sin cancelar"; un viaje en curso no muestra el botón | ✅ |
| Reconocer antes que recordar | Motivos de cancelación sugeridos en una lista; "Otro motivo" solo si ninguno sirve | ✅ |
| Botones fáciles de tocar | Botones principales de ancho completo y de 48 px o más de alto en el celular | ✅ |
| Mensajes claros y en español | Errores y avisos redactados para el usuario ("Un viaje en curso no se puede cancelar.") | ✅ |
| Consistencia | Mismos colores por estado en todas las pantallas (Publicado amarillo, Aceptado celeste, En curso verde, Finalizado gris, Cancelado rojo) | ✅ |
| Tema claro y oscuro | Sigue la configuración del dispositivo | ✅ |

Pendiente para una próxima etapa: una prueba con usuarios reales (no se hizo en este proyecto).

### 5.3 Seguridad y HTTPS (RNF03 y RNF04)

| Prueba | Cómo se probó | Resultado |
|---|---|---|
| Una petición por HTTP se redirige a HTTPS | `curl http://localhost:5003/Cuenta/Login` | ✅ 307 a `https://localhost:7090/Cuenta/Login` |
| HSTS de un año en producción | Configuración `AddHsts` (365 días) y `UseHsts` fuera de Desarrollo | ✅ (ASP.NET no lo envía a `localhost` a propósito) |
| Encabezados de seguridad en cada respuesta | `curl -D -` y prueba U35 | ✅ X-Content-Type-Options, X-Frame-Options, Content-Security-Policy, Referrer-Policy, Permissions-Policy |
| Protección CSRF | POST al inicio de sesión sin el token antiforgery | ✅ 400 Bad Request |
| Páginas protegidas sin sesión | POST a `/Pasajero/Cancelar/1` sin iniciar sesión | ✅ Redirige al inicio de sesión |
| Cookies de sesión | Revisión de `Set-Cookie` | ✅ `httponly` y `samesite` |
| Acceso por rol | Pasajero entra al historial del conductor; conductor a la pantalla de viaje de un pasajero; conductor a documentos (sprints 3 y 4) | ✅ Acceso denegado |
| Un usuario solo ve y cancela sus viajes | Prueba U24 y controles de pertenencia en cada acción | ✅ |
| Contraseñas y bloqueo | Identity guarda el hash; 5 intentos fallidos bloquean la cuenta | ✅ |
| Inyección SQL | Todas las consultas pasan por Entity Framework con parámetros | ✅ (revisión de código) |

### 5.4 Compatibilidad entre navegadores y dispositivos (RNF06)

| Navegador / motor | Celular | Escritorio | Resultado |
|---|---|---|---|
| Chrome (Blink: también Edge, Opera, Samsung Internet) | ✅ | ✅ | 100 % de los casos |
| WebKit (Safari en iPhone, iPad y Mac) | ✅ | ✅ | 100 % de los casos |
| Firefox (Gecko) | No se pudo automatizar | No se pudo automatizar | Windows bloqueó el ejecutable de Firefox de Playwright en esta computadora; queda para probar a mano |

Durante esta prueba apareció un problema en WebKit: las pantallas que se actualizan solas usaban `<meta http-equiv="refresh">` y WebKit volvía a cargar la página mientras se tocaba un botón, por lo que "Aceptar viaje" no llegaba a enviarse. Se reemplazó por una recarga con JavaScript que espera si la persona está escribiendo y no corre con la pestaña oculta (ver mantenimiento). Después del cambio, todos los casos pasan en los dos motores.

### 5.5 Términos y condiciones (RNF08)

- Página pública `/Home/Terminos` con: qué es la plataforma, quién puede usarla, compromisos del pasajero y del conductor, cancelaciones, ausencia de pagos, suspensión de cuentas, privacidad según la Ley 25.326, responsabilidad y cambios.
- Casilla obligatoria en los dos registros (pasajero y conductor), validada en el navegador y en el servidor (`[DebeAceptarse]`).
- Se guarda la fecha de aceptación en `Users.FechaAceptacionTerminos`.
- Enlace a los términos desde el inicio de sesión y desde el registro.

## 6. Mantenimiento

### 6.1 Cambios de los sprints 5 y 6

| Fecha | Tipo | Cambio | Motivo |
|---|---|---|---|
| 19/11/2026 | Perfectivo | Cancelación con motivo para pasajero y conductor; se guarda quién canceló y cuándo (migración `CancelacionYTerminos`) | RF09 |
| 19/11/2026 | Preventivo | La cancelación es una actualización condicional en la base | Si el conductor inicia el viaje justo cuando el pasajero cancela, solo una de las dos acciones se aplica |
| 19/11/2026 | Perfectivo | Historial de viajes del pasajero y del conductor, con duración y motivo de cancelación | RF10 (sin importe, el alcance no maneja dinero) |
| 19/11/2026 | Perfectivo | La pantalla del viaje del conductor se actualiza sola mientras espera, para ver si el pasajero canceló | RF09 |
| 19/11/2026 | Perfectivo | El administrador ve quién canceló y el motivo en el listado de viajes | RF11 |
| 19/11/2026 | Perfectivo | Términos y condiciones con aceptación obligatoria y fecha guardada | RNF08 |
| 19/11/2026 | Preventivo | Encabezados de seguridad, HSTS de un año, cookies `HttpOnly` y `SameSite` | RNF03 |
| 19/11/2026 | Correctivo | Recarga automática con JavaScript en lugar de `meta refresh` | En WebKit la recarga interrumpía los botones (hallado en la prueba de compatibilidad) |

### 6.2 Cambios de los sprints 2 a 4

Ver la sección 3 de [registro-pruebas-y-mantenimiento.md](registro-pruebas-y-mantenimiento.md): vistas según el prototipo, interfaz responsive, mensajes de Identity en español, documentos fuera de `wwwroot`, aceptación condicional, suspensión de cuentas, actualización automática y quita de la tarifa.

### 6.3 Plan de mantenimiento a futuro

- **Correctivo:** registrar los errores con los logs de ASP.NET Core y reproducir cada uno con una prueba xUnit antes de corregirlo.
- **Adaptativo:** actualizar .NET y los paquetes de Entity Framework e Identity cuando salgan versiones con soporte; volver a correr las pruebas unitarias y las de punta a punta en cada actualización.
- **Perfectivo:** notificaciones en tiempo real (SignalR) en lugar de la actualización cada 15 a 20 segundos; ubicación del pasajero en un mapa.
- **Preventivo:** copia de seguridad diaria de la base, revisión trimestral de paquetes con vulnerabilidades (`dotnet list package --vulnerable`) y vencimientos de licencias y seguros.

## 7. Conclusión

Los sprints 5 y 6 quedaron completos. Las 43 pruebas unitarias y los 37 casos de punta a punta pasan; las pantallas cargan en menos de 130 ms en red local; la aplicación funciona igual en los motores de Chrome y Safari, en celular y escritorio; el tráfico se fuerza a HTTPS con encabezados de seguridad; y el registro exige aceptar los términos y condiciones. Queda pendiente probar a mano en Firefox y hacer una prueba de usabilidad con usuarios reales.
