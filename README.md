# VuelvoSeguro

Plataforma web de conductores designados: la persona que bebió publica su viaje y un conductor verificado la va a buscar y la lleva a su casa **en el auto del propio pasajero**. Así vuelven la persona y su vehículo, sin que nadie maneje alcoholizado.

## Datos de la entrega

| | |
|---|---|
| **Informe** | Entrega final: proyecto terminado, pruebas, implementación y evaluación |
| **Materia** | Taller de Proyectos II |
| **Carrera** | Tecnicatura Universitaria en Tecnologías de la Información |
| **Profesor** | Christian Pardavila |
| **Fecha de entrega** | 17/11/2026 |
| **Repositorio** | https://github.com/RamonJVillarroel/VuelvoSeguro |

### Integrantes

| Nombre y apellido | Correo |
|---|---|
| Ramón Villarroel | rvillarroel4@alumnos.kennedy.edu.ar |

## Estado de avance: 100 %

El proyecto sigue un ciclo de vida incremental gestionado con Scrum (Avance 1, Tabla 3). Los **6 sprints** de desarrollo planificados están **terminados**.

| Sprint | Incremento | Requerimientos | Entrega | Estado |
|---|---|---|---|---|
| 1 | Análisis y diseño: flujo general, casos de uso, interfaz (prototipo) y base de datos | Todos | Avance 2 | ✅ Terminado |
| 2 | Cuentas, roles y vehículo del pasajero; repositorio Git | RF01, RF02, RF03 | Avance 2 | ✅ Terminado |
| 3 | Validación de conductores y panel de administración | RF04, RF11 | Avance 3 | ✅ Terminado |
| 4 | Publicación, listado, aceptación y estados de los viajes | RF05, RF07, RF08 | Avance 3 | ✅ Terminado |
| 5 | Cancelación con motivo e historial de viajes | RF09, RF10 | Entrega final | ✅ Terminado |
| 6 | Pruebas, implementación y evaluación | RNF01 a RNF08 | Entrega final | ✅ Terminado |

**Alcance acotado:** el proyecto no maneja dinero. Por eso queda fuera RF06 (tarifa estimada) y el importe del historial (RF10). Tampoco hay calificaciones ni pagos.

### Funcionalidades

- **Usuario:** registro como pasajero o conductor (rechaza menores de 18 años, DNI y correo únicos, exige aceptar los términos y condiciones) e inicio de sesión que lleva a cada rol a sus pantallas.
- **Pasajero:** carga y edición de su auto (patente, caja, vencimiento del seguro); publica el viaje a su casa con un clic indicando dónde está; sigue el estado del viaje (buscando conductor, conductor en camino, en curso, llegada); cancela el viaje eligiendo un motivo mientras no haya empezado; ve el historial de sus viajes.
- **Conductor:** envía número y vencimiento de licencia, foto de la licencia, foto del DNI y certificado de antecedentes penales; mientras no esté aprobado solo ve la pantalla de espera; ya aprobado ve los viajes publicados, acepta uno y avanza su estado hasta finalizarlo; puede cancelarlo con un motivo antes de recoger al pasajero; ve el historial de sus viajes.
- **Administrador:** revisa a cada conductor pendiente con sus documentos y lo aprueba o rechaza; ve todos los viajes y todos los usuarios; suspende y reactiva cuentas (un usuario suspendido no puede ingresar); en Viajes ve quién canceló cada viaje y por qué.

Estados del viaje (RF08): **Publicado → Aceptado → En curso → Finalizado**, o **Cancelado** si el pasajero o el conductor lo cancelan antes de que empiece (RF09). Un viaje En curso no se puede cancelar.

### Requerimientos no funcionales

| Requerimiento | Cómo se cumple |
|---|---|
| RNF01 Rendimiento | Las pantallas cargan en menos de 130 ms en red local; las consultas de viajes responden en menos de 1 s con 1000 viajes |
| RNF02 Usabilidad | Pedir un viaje lleva un clic; estados visibles; motivos de cancelación sugeridos; botones grandes para el celular |
| RNF03 Seguridad y HTTPS | Redirección a HTTPS, HSTS, encabezados de seguridad, protección CSRF, cookies HttpOnly, acceso por rol |
| RNF04 Privacidad | Documentos de los conductores fuera de `wwwroot`, solo los ve el administrador (Ley 25.326) |
| RNF06 Compatibilidad | Probado en los motores de Chrome/Edge y Safari, en celular y escritorio |
| RNF08 Términos y condiciones | Página pública y aceptación obligatoria al registrarse, con la fecha guardada |

El detalle de cada prueba está en el [informe de pruebas y mantenimiento](docs/informe-pruebas-y-mantenimiento.md).

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
   Al iniciar también se aplican las migraciones pendientes (por ejemplo `CancelacionYTerminos`, del sprint 5).
4. Para correr las pruebas unitarias: menú **Prueba > Ejecutar todas las pruebas** (o `dotnet test` en la carpeta `VuelvoSeguro`). Son 43 pruebas.
5. Pruebas de punta a punta (opcional): con la aplicación corriendo en el perfil `https`, en la carpeta `pruebas-e2e` ejecutar `npm install`, `npx playwright-core install webkit` y `node e2e.js`.

## Estructura

```
VuelvoSeguro/
├── VuelvoSeguro/              Aplicación web (MVC)
│   ├── Controllers/           Cuenta, Pasajero, Conductor, Admin
│   ├── Models/                Usuario, Rol, Vehiculo, Viaje, EstadoUsuario, EstadoViaje
│   ├── Servicios/             ViajeService (reglas del viaje), AlmacenDocumentos, EncabezadosSeguridad
│   ├── ViewModels/            Datos de los formularios con sus validaciones
│   ├── Views/                 Pantallas según el prototipo
│   └── Data/                  DbContext, datos iniciales y migraciones
└── VuelvoSeguro.Tests/        Pruebas unitarias (xUnit): viajes, cancelación, historial, validaciones, rendimiento
docs/
├── informe-pruebas-y-mantenimiento.md   Informe final (sprint 6)
├── registro-pruebas-y-mantenimiento.md  Registro de pruebas por sprint
└── capturas/                            Capturas de las pruebas de punta a punta
pruebas-e2e/                   Pruebas de punta a punta con Playwright
```

## Organización del trabajo

- Una rama por sprint (`sprint-2` a `sprint-5`) que se integra a `main` con un pull request al terminar el sprint.
- Cada commit indica el sprint y los requerimientos que cubre.
- Las pruebas realizadas y los cambios de mantenimiento se registran en [docs/registro-pruebas-y-mantenimiento.md](docs/registro-pruebas-y-mantenimiento.md) y se resumen en el [informe de pruebas y mantenimiento](docs/informe-pruebas-y-mantenimiento.md).
