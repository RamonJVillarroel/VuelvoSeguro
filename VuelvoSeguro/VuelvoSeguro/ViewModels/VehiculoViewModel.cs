using System.ComponentModel.DataAnnotations;

namespace VuelvoSeguro.ViewModels
{
    // RF03: vehículo del pasajero.
    public class VehiculoViewModel
    {
        [Required(ErrorMessage = "Ingresá la marca.")]
        [StringLength(40)]
        public string Marca { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá el modelo.")]
        [StringLength(40)]
        public string Modelo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá la patente.")]
        [RegularExpression(@"^([A-Za-z]{3}\s?\d{3}|[A-Za-z]{2}\s?\d{3}\s?[A-Za-z]{2})$",
            ErrorMessage = "La patente tiene que ser como ABC 123 o AB 123 CD.")]
        public string Patente { get; set; } = string.Empty;

        [Display(Name = "Tipo de caja")]
        public bool EsAutomatico { get; set; }

        [Required(ErrorMessage = "Ingresá el vencimiento del seguro.")]
        [DataType(DataType.Date)]
        [Display(Name = "Vencimiento del seguro")]
        public DateTime? VencimientoSeguro { get; set; }

        // Guarda la patente sin espacios y en mayúsculas.
        public static string NormalizarPatente(string patente) =>
            patente.Replace(" ", string.Empty).ToUpperInvariant();
    }
}
