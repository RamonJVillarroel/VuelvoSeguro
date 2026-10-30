using System.ComponentModel.DataAnnotations;

namespace VuelvoSeguro.Models
{
    public class EstadoUsuario
    {
        public const int Activo = 1;
        public const int Pendiente = 2;
        public const int Aprobado = 3;
        public const int Rechazado = 4;
        public const int Suspendido = 5;

        public int Id { get; set; }

        [Required, StringLength(30)]
        public string NombreEstado { get; set; } = string.Empty;

        public ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}
