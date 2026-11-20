using Microsoft.EntityFrameworkCore;
using VuelvoSeguro.Data;
using VuelvoSeguro.Models;

namespace VuelvoSeguro.Servicios
{
    public record Resultado(bool Ok, string? Error = null, int? ViajeId = null)
    {
        public static Resultado Exito(int? viajeId = null) => new(true, null, viajeId);
        public static Resultado Falla(string error) => new(false, error);
    }

    // Reglas del ciclo de vida del viaje (RF05, RF07, RF08, RF09 y RF10):
    // Publicado -> Aceptado -> En curso -> Finalizado, o Cancelado antes de que empiece.
    public class ViajeService
    {
        public static readonly int[] EstadosActivos = { EstadoViaje.Publicado, EstadoViaje.Aceptado, EstadoViaje.EnCurso };

        // RF09: solo se cancela un viaje que todavía no empezó.
        public static readonly int[] EstadosCancelables = { EstadoViaje.Publicado, EstadoViaje.Aceptado };

        public const int LargoMaximoMotivo = 200;

        // Texto de origen cuando hay coordenadas del celular pero no se pudo obtener la dirección.
        public const string UbicacionSinDireccion = "Ubicación del celular (ver mapa)";

        public static bool CoordenadasValidas(double latitud, double longitud) =>
            double.IsFinite(latitud) && double.IsFinite(longitud)
            && latitud is >= -90 and <= 90 && longitud is >= -180 and <= 180;

        // Motivos sugeridos en la pantalla de cancelación ("Otro" deja escribir uno propio).
        public static readonly string[] MotivosPasajero =
        {
            "Ya conseguí otra forma de volver",
            "Me quedo un rato más",
            "Me equivoqué al pedir el viaje",
            "El conductor tarda demasiado"
        };

        public static readonly string[] MotivosConductor =
        {
            "Tuve un imprevisto",
            "No encuentro al pasajero",
            "Estoy demasiado lejos",
            "Problema con el auto del pasajero"
        };

        private readonly ApplicationDbContext _db;

        public ViajeService(ApplicationDbContext db)
        {
            _db = db;
        }

        // RF05: el pasajero publica el viaje desde donde está hasta su casa, en su propio auto.
        // El origen se toma de la ubicación del celular (latitud/longitud). Si el pasajero no dio
        // permiso, el texto que escribió es obligatorio; si dio permiso, el texto es opcional.
        public async Task<Resultado> PublicarAsync(int pasajeroId, string? origen, DateTime ahora, double? latitud = null, double? longitud = null)
        {
            var tieneUbicacion = latitud.HasValue && longitud.HasValue;
            if (tieneUbicacion && !CoordenadasValidas(latitud!.Value, longitud!.Value))
            {
                return Resultado.Falla("La ubicación del celular no es válida. Escribí dónde estás.");
            }
            if (string.IsNullOrWhiteSpace(origen))
            {
                if (!tieneUbicacion)
                {
                    return Resultado.Falla("No pudimos tomar tu ubicación. Escribí dónde estás para que el conductor te encuentre.");
                }
                origen = UbicacionSinDireccion;
            }
            origen = origen.Trim();
            if (origen.Length > 150)
            {
                return Resultado.Falla("La dirección de origen es demasiado larga.");
            }

            var pasajero = await _db.Users.Include(u => u.Vehiculos).FirstOrDefaultAsync(u => u.Id == pasajeroId);
            if (pasajero is null)
            {
                return Resultado.Falla("No encontramos tu cuenta.");
            }
            if (string.IsNullOrWhiteSpace(pasajero.Direccion))
            {
                return Resultado.Falla("Cargá la dirección de tu casa antes de pedir un viaje.");
            }
            var vehiculo = pasajero.Vehiculos.FirstOrDefault();
            if (vehiculo is null || !vehiculo.TieneSeguroVigente(ahora))
            {
                return Resultado.Falla("Para pedir un viaje necesitás un auto con el seguro vigente.");
            }
            if (await ViajeActivoDelPasajeroAsync(pasajeroId) is not null)
            {
                return Resultado.Falla("Ya tenés un viaje activo.");
            }

            var viaje = new Viaje
            {
                PasajeroId = pasajeroId,
                VehiculoId = vehiculo.Id,
                DireccionOrigen = origen,
                OrigenLatitud = tieneUbicacion ? latitud : null,
                OrigenLongitud = tieneUbicacion ? longitud : null,
                DireccionDestino = pasajero.Direccion,
                FechaPublicacion = ahora,
                EstadoViajeId = EstadoViaje.Publicado
            };
            _db.Viajes.Add(viaje);
            await _db.SaveChangesAsync();
            return Resultado.Exito(viaje.Id);
        }

        // RF07: viajes publicados que todavía no tienen conductor, del más viejo al más nuevo.
        public Task<List<Viaje>> DisponiblesAsync()
        {
            return ConDatos()
                .Where(v => v.EstadoViajeId == EstadoViaje.Publicado && v.ConductorId == null)
                .OrderBy(v => v.FechaPublicacion)
                .ToListAsync();
        }

        // RF07: un viaje queda asignado a un único conductor aprobado.
        public async Task<Resultado> AceptarAsync(int viajeId, int conductorId)
        {
            var conductor = await _db.Users.FirstOrDefaultAsync(u => u.Id == conductorId);
            if (conductor is null || conductor.EstadoUsuarioId != EstadoUsuario.Aprobado)
            {
                return Resultado.Falla("Solo un conductor aprobado puede aceptar viajes.");
            }
            if (await ViajeActivoDelConductorAsync(conductorId) is not null)
            {
                return Resultado.Falla("Ya tenés un viaje en curso. Terminalo antes de aceptar otro.");
            }

            // La actualización es condicional: si dos conductores aceptan a la vez, solo uno la logra.
            var filas = await _db.Viajes
                .Where(v => v.Id == viajeId && v.EstadoViajeId == EstadoViaje.Publicado && v.ConductorId == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(v => v.ConductorId, conductorId)
                    .SetProperty(v => v.EstadoViajeId, EstadoViaje.Aceptado));
            return filas == 1
                ? Resultado.Exito(viajeId)
                : Resultado.Falla("Ese viaje ya no está disponible: lo aceptó otro conductor o se canceló.");
        }

        // RF08: el conductor confirma que recogió al pasajero y empieza el viaje.
        public Task<Resultado> IniciarAsync(int viajeId, int conductorId, DateTime ahora) =>
            CambiarEstadoAsync(viajeId, conductorId, EstadoViaje.Aceptado, EstadoViaje.EnCurso,
                v => v.FechaViajeInicio = ahora, "El viaje no se puede iniciar en su estado actual.");

        // RF08: el conductor llegó al destino y finaliza el viaje.
        public Task<Resultado> FinalizarAsync(int viajeId, int conductorId, DateTime ahora) =>
            CambiarEstadoAsync(viajeId, conductorId, EstadoViaje.EnCurso, EstadoViaje.Finalizado,
                v => v.FechaViajeFin = ahora, "El viaje no se puede finalizar en su estado actual.");

        // RF09: el pasajero o el conductor cancela el viaje indicando el motivo.
        // Un viaje En curso no se puede cancelar: el pasajero ya está arriba de su auto.
        public async Task<Resultado> CancelarAsync(int viajeId, int usuarioId, string? motivo, DateTime ahora)
        {
            motivo = motivo?.Trim();
            if (string.IsNullOrEmpty(motivo))
            {
                return Resultado.Falla("Contanos por qué cancelás el viaje.");
            }
            if (motivo.Length > LargoMaximoMotivo)
            {
                return Resultado.Falla($"El motivo puede tener hasta {LargoMaximoMotivo} caracteres.");
            }

            var viaje = await _db.Viajes.AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == viajeId && (v.PasajeroId == usuarioId || v.ConductorId == usuarioId));
            if (viaje is null)
            {
                return Resultado.Falla("No encontramos ese viaje entre los tuyos.");
            }
            if (viaje.EstadoViajeId == EstadoViaje.EnCurso)
            {
                return Resultado.Falla("Un viaje en curso no se puede cancelar.");
            }
            if (!EstadosCancelables.Contains(viaje.EstadoViajeId))
            {
                return Resultado.Falla("Este viaje ya terminó.");
            }

            // Actualización condicional: si mientras tanto el conductor inició el viaje, la cancelación no se aplica.
            var filas = await _db.Viajes
                .Where(v => v.Id == viajeId && EstadosCancelables.Contains(v.EstadoViajeId))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(v => v.EstadoViajeId, EstadoViaje.Cancelado)
                    .SetProperty(v => v.MotivoCancelacion, motivo)
                    .SetProperty(v => v.FechaCancelacion, ahora)
                    .SetProperty(v => v.CanceladoPorId, usuarioId));
            return filas == 1
                ? Resultado.Exito(viajeId)
                : Resultado.Falla("El viaje cambió de estado y ya no se puede cancelar.");
        }

        // RF10: viajes terminados (finalizados o cancelados) del usuario, como pasajero o como conductor, del más nuevo al más viejo.
        public Task<List<Viaje>> HistorialAsync(int usuarioId)
        {
            return ConDatos()
                .Where(v => (v.PasajeroId == usuarioId || v.ConductorId == usuarioId)
                    && (v.EstadoViajeId == EstadoViaje.Finalizado || v.EstadoViajeId == EstadoViaje.Cancelado))
                .OrderByDescending(v => v.FechaPublicacion)
                .ThenByDescending(v => v.Id)
                .ToListAsync();
        }

        public Task<Viaje?> ViajeActivoDelPasajeroAsync(int pasajeroId) =>
            ConDatos().FirstOrDefaultAsync(v => v.PasajeroId == pasajeroId && EstadosActivos.Contains(v.EstadoViajeId));

        public Task<Viaje?> ViajeActivoDelConductorAsync(int conductorId) =>
            ConDatos().FirstOrDefaultAsync(v => v.ConductorId == conductorId && EstadosActivos.Contains(v.EstadoViajeId));

        public Task<Viaje?> BuscarAsync(int viajeId) => ConDatos().FirstOrDefaultAsync(v => v.Id == viajeId);

        private async Task<Resultado> CambiarEstadoAsync(int viajeId, int conductorId, int desde, int hasta, Action<Viaje> cambio, string error)
        {
            var viaje = await _db.Viajes.FirstOrDefaultAsync(v => v.Id == viajeId && v.ConductorId == conductorId);
            if (viaje is null)
            {
                return Resultado.Falla("No encontramos ese viaje entre los tuyos.");
            }
            if (viaje.EstadoViajeId != desde)
            {
                return Resultado.Falla(error);
            }
            viaje.EstadoViajeId = hasta;
            cambio(viaje);
            await _db.SaveChangesAsync();
            return Resultado.Exito(viajeId);
        }

        private IQueryable<Viaje> ConDatos() => _db.Viajes
            .Include(v => v.Pasajero)
            .Include(v => v.Conductor)
            .Include(v => v.Vehiculo)
            .Include(v => v.EstadoViaje)
            .Include(v => v.CanceladoPor);
    }
}
