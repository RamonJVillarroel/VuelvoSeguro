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

    // Reglas del ciclo de vida del viaje (RF05, RF07 y RF08):
    // Publicado -> Aceptado -> En curso -> Finalizado.
    public class ViajeService
    {
        public static readonly int[] EstadosActivos = { EstadoViaje.Publicado, EstadoViaje.Aceptado, EstadoViaje.EnCurso };

        private readonly ApplicationDbContext _db;

        public ViajeService(ApplicationDbContext db)
        {
            _db = db;
        }

        // RF05: el pasajero publica el viaje desde donde está hasta su casa, en su propio auto.
        public async Task<Resultado> PublicarAsync(int pasajeroId, string? origen, DateTime ahora)
        {
            if (string.IsNullOrWhiteSpace(origen))
            {
                return Resultado.Falla("Ingresá dónde estás para que el conductor te encuentre.");
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
            .Include(v => v.EstadoViaje);
    }
}
