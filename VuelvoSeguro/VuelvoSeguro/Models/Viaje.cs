using System.ComponentModel.DataAnnotations;

namespace VuelvoSeguro.Models
{
    // Un viaje tiene 1 pasajero, 0..1 conductor y usa el auto del pasajero.
    public class Viaje
    {
        public int Id { get; set; }

        public DateTime FechaPublicacion { get; set; }
        public DateTime? FechaViajeInicio { get; set; }
        public DateTime? FechaViajeFin { get; set; }

        [Required, StringLength(150)]
        public string DireccionOrigen { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string DireccionDestino { get; set; } = string.Empty;

        [StringLength(200)]
        public string? MotivoCancelacion { get; set; }

        public int PasajeroId { get; set; }
        public Usuario? Pasajero { get; set; }

        public int? ConductorId { get; set; }
        public Usuario? Conductor { get; set; }

        public int VehiculoId { get; set; }
        public Vehiculo? Vehiculo { get; set; }

        public int EstadoViajeId { get; set; }
        public EstadoViaje? EstadoViaje { get; set; }
    }
}
