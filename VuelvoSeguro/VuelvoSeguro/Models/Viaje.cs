using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

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

        // Ubicación del celular del pasajero al publicar (navigator.geolocation del navegador).
        // Es null si el pasajero no dio permiso y escribió el origen a mano.
        public double? OrigenLatitud { get; set; }
        public double? OrigenLongitud { get; set; }

        [NotMapped]
        public bool TieneUbicacion => OrigenLatitud.HasValue && OrigenLongitud.HasValue;

        // Links gratuitos a Google Maps con las coordenadas (no necesitan API key).
        [NotMapped]
        public string? LinkMapaOrigen => TieneUbicacion
            ? "https://www.google.com/maps/search/?api=1&query=" + Coordenadas()
            : null;

        [NotMapped]
        public string? LinkComoLlegarOrigen => TieneUbicacion
            ? "https://www.google.com/maps/dir/?api=1&travelmode=walking&destination=" + Coordenadas()
            : null;

        private string Coordenadas() => string.Format(CultureInfo.InvariantCulture, "{0:0.######},{1:0.######}", OrigenLatitud, OrigenLongitud);

        [Required, StringLength(150)]
        public string DireccionDestino { get; set; } = string.Empty;

        // RF09: quién canceló el viaje, cuándo y por qué.
        [StringLength(200)]
        public string? MotivoCancelacion { get; set; }
        public DateTime? FechaCancelacion { get; set; }
        public int? CanceladoPorId { get; set; }
        public Usuario? CanceladoPor { get; set; }

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
