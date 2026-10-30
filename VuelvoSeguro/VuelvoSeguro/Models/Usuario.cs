using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace VuelvoSeguro.Models
{
    // Users del modelo de clases. Email, teléfono y contraseña los maneja Identity.
    public class Usuario : IdentityUser<int>
    {
        [Required, StringLength(60)]
        public string Nombre { get; set; } = string.Empty;

        [Required, StringLength(60)]
        public string Apellido { get; set; } = string.Empty;

        [Required, StringLength(10)]
        public string Dni { get; set; } = string.Empty;

        public DateTime FechaNacimiento { get; set; }

        // Dirección de la casa del pasajero: destino de sus viajes.
        [StringLength(150)]
        public string? Direccion { get; set; }

        // Datos del conductor (se completan en el sprint 3).
        [StringLength(20)]
        public string? NumeroLicencia { get; set; }
        public DateTime? VencimientoLicencia { get; set; }
        public DateTime? FechaAprobacion { get; set; }
        public string? RutaDni { get; set; }
        public string? RutaLicencia { get; set; }
        public string? RutaAntecedentes { get; set; }

        public int EstadoUsuarioId { get; set; }
        public EstadoUsuario? EstadoUsuario { get; set; }

        public ICollection<Vehiculo> Vehiculos { get; set; } = new List<Vehiculo>();
        public ICollection<Viaje> ViajesComoPasajero { get; set; } = new List<Viaje>();
        public ICollection<Viaje> ViajesComoConductor { get; set; } = new List<Viaje>();

        public string NombreCompleto => $"{Nombre} {Apellido}";
    }
}
