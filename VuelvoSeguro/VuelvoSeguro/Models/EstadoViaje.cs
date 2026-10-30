using System.ComponentModel.DataAnnotations;

namespace VuelvoSeguro.Models
{
    public class EstadoViaje
    {
        public const int Publicado = 1;
        public const int Aceptado = 2;
        public const int EnCurso = 3;
        public const int Finalizado = 4;
        public const int Cancelado = 5;

        public int Id { get; set; }

        [Required, StringLength(30)]
        public string NombreEstado { get; set; } = string.Empty;

        public ICollection<Viaje> Viajes { get; set; } = new List<Viaje>();
    }
}
