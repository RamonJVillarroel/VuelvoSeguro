using System.ComponentModel.DataAnnotations;

namespace VuelvoSeguro.Models
{
    public class Vehiculo
    {
        public int Id { get; set; }

        [Required, StringLength(40)]
        public string Marca { get; set; } = string.Empty;

        [Required, StringLength(40)]
        public string Modelo { get; set; } = string.Empty;

        [Required, StringLength(10)]
        public string Patente { get; set; } = string.Empty;

        public bool EsAutomatico { get; set; }

        public DateTime VencimientoSeguro { get; set; }

        public int PropietarioId { get; set; }
        public Usuario? Propietario { get; set; }

        public bool TieneSeguroVigente(DateTime hoy) => VencimientoSeguro.Date >= hoy.Date;
    }
}
