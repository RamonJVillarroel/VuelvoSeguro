# VuelvoSeguro

Plataforma web de conductores designados: la persona que bebió publica su viaje y un conductor verificado la va a buscar y la lleva a su casa **en el auto del propio pasajero**. Así vuelven la persona y su vehículo, sin que nadie maneje alcoholizado.

## Datos de la entrega

| | |
|---|---|
| **Informe** | Avance 3: Proyecto en Git y avance del desarrollo |
| **Materia** | Taller de Proyectos II |
| **Carrera** | Tecnicatura Universitaria en Tecnologías de la Información |
| **Profesor** | Christian Pardavila |
| **Fecha de entrega** | 17/11/2026 |
| **Repositorio** | https://github.com/RamonJVillarroel/VuelvoSeguro |

### Integrantes

| Nombre y apellido | Correo |
|---|---|---|---|
| Ramón Villarroel | rvillarroel4@alumnos.kennedy.edu.ar |

El proyecto lo desarrolla un único integrante, que realizó todas las tareas de análisis, diseño, desarrollo y pruebas.

## Estado de avance: 67 %

El proyecto sigue un ciclo de vida incremental gestionado con Scrum (Avance 1, Tabla 3). De los 6 sprints de desarrollo planificados hay **4 terminados**, que cubren 8 de los 10 requerimientos funcionales del alcance.

| Sprint | Incremento | Requerimientos | Entrega | Estado |
|---|---|---|---|---|
| 1 | Análisis y diseño: flujo general, casos de uso, interfaz (prototipo) y base de datos | Todos | Avance 2 | ✅ Terminado |
| 2 | Cuentas, roles y vehículo del pasajero; repositorio Git | RF01, RF02, RF03 | Avance 2 | ✅ Terminado |
| 3 | Validación de conductores y panel de administración | RF04, RF11 | Avance 3 | ✅ Terminado |
| 4 | Publicación, listado, aceptación y estados de los viajes | RF05, RF07, RF08 | Avance 3 | ✅ Terminado |
| 5 | Cancelación con motivo e historial de viajes | RF09, RF10 | Entrega final | ⏳ Pendiente |
| 6 | Pruebas, implementación y evaluación | RNF01 a RNF08 | Entrega final | 🟡 Iniciado (pruebas unitarias del flujo de viajes) |

**Alcance acotado:** el proyecto no maneja dinero. Por eso queda fuera RF06 (tarifa estimada) y el importe del historial (RF10). Tampoco hay calificaciones ni pagos.

### Qué funciona hoy

- **Usuario:** registro como pasajero o conductor (rechaza menores de 18 años, DNI y correo únicos) e inicio de sesión que lleva a cada rol a sus pantallas.
- **Pasajero:** carga y edición de su auto (patente, caja, vencimiento del seguro); publica el viaje a su casa con un clic indicando dónde está; sigue el estado del viaje (buscando conductor, conductor en camino, en curso, llegada).
- **Conductor:** envía número y vencimiento de licencia, foto de la licencia, foto del DNI y certificado de antecedentes penales; mientras no esté aprobado solo ve la pantalla de espera; ya aprobado ve los viajes publicados, acepta uno y avanza su estado hasta finalizarlo.
- **Administrador:** revisa a cada conductor pendiente con sus documentos y lo aprueba o rechaza; ve todos los viajes y todos los usuarios; suspende y reactiva cuentas (un usuario suspendido no puede ingresar).

Estados del viaje (RF08): **Publicado → Aceptado → En curso → Finalizado** (y Cancelado, que se completa en el sprint 5).

## Tecnologías

- ASP.NET Core MVC (.NET 9) en C#, con vistas Razor
- ASP.NET Core Identity (usuarios, roles y contraseñas con hash)
- Entity Framework Core con SQL Server Express (migraciones en `Data/Migrations`)
- Bootstrap 5 y JavaScript, interfaz responsive para celular, tablet y escritorio
- xUnit para las pruebas unitarias
- Git y GitHub para el control de versiones

## Cómo ejecutarlo

1. Abrir `VuelvoSeguro/VuelvoSeguro.sln` con Visual Studio 2022.
2. Tener SQL Server Express instalado (la cadena de conexión está en `appsettings.json`: `Server=.\SQLEXPRESS;Database=VuelvoSeguroDb`).
3. Ejecutar el proyecto `VuelvoSeguro`. Al iniciar, la aplicación crea la base de datos, los roles y un administrador inicial:
   - Correo: `admin@vuelvoseguro.com`
   - Contraseña: `Admin123!` (definida en `appsettings.Development.json`, solo para desarrollo)
4. Para correr las pruebas: menú **Prueba > Ejecutar todas las pruebas** (o `dotnet test` en la carpeta `VuelvoSeguro`).

## Estructura

```
VuelvoSeguro/
├── VuelvoSeguro/              Aplicación web (MVC)
│   ├── Controllers/           Cuenta, Pasajero, Conductor, Admin
│   ├── Models/                Usuario, Rol, Vehiculo, Viaje, EstadoUsuario, EstadoViaje
│   ├── Servicios/             ViajeService (reglas del viaje), AlmacenDocumentos
│   ├── ViewModels/            Datos de los formularios con sus validaciones
│   ├── Views/                 Pantallas según el prototipo
│   └── Data/                  DbContext, datos iniciales y migraciones
└── VuelvoSeguro.Tests/        Pruebas unitarias (xUnit)
docs/
└── registro-pruebas-y-mantenimiento.md
```

## Organización del trabajo

- Una rama por sprint (`sprint-2`, `sprint-3`, `sprint-4`) que se integra a `main` con un pull request al terminar el sprint.
- Cada commit indica el sprint y los requerimientos que cubre.
- Las pruebas realizadas y los cambios de mantenimiento se registran en [docs/registro-pruebas-y-mantenimiento.md](docs/registro-pruebas-y-mantenimiento.md).
