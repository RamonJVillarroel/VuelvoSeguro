using System.ComponentModel.DataAnnotations;

namespace VuelvoSeguro.ViewModels
{
    // RF04: datos de la licencia y los tres documentos que el conductor envía para que lo validen.
    public class DocumentacionViewModel : IValidatableObject
    {
        public const long TamanioMaximo = 5 * 1024 * 1024;
        public static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".pdf" };

        [Required(ErrorMessage = "Ingresá el número de tu licencia.")]
        [StringLength(20)]
        [Display(Name = "Número de licencia")]
        public string NumeroLicencia { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá el vencimiento de tu licencia.")]
        [DataType(DataType.Date)]
        [Display(Name = "Vencimiento de la licencia")]
        public DateTime? VencimientoLicencia { get; set; }

        [Required(ErrorMessage = "Subí la foto de tu licencia.")]
        [Display(Name = "Foto de la licencia (frente y dorso)")]
        public IFormFile? FotoLicencia { get; set; }

        [Required(ErrorMessage = "Subí la foto de tu DNI.")]
        [Display(Name = "Foto del DNI")]
        public IFormFile? FotoDni { get; set; }

        [Required(ErrorMessage = "Subí el certificado de antecedentes penales.")]
        [Display(Name = "Certificado de antecedentes penales")]
        public IFormFile? Antecedentes { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (VencimientoLicencia.HasValue && VencimientoLicencia.Value.Date < DateTime.Today)
            {
                yield return new ValidationResult("La licencia está vencida.", new[] { nameof(VencimientoLicencia) });
            }
            foreach (var (archivo, campo) in new[] { (FotoLicencia, nameof(FotoLicencia)), (FotoDni, nameof(FotoDni)), (Antecedentes, nameof(Antecedentes)) })
            {
                var error = ValidarArchivo(archivo);
                if (error != null)
                {
                    yield return new ValidationResult(error, new[] { campo });
                }
            }
        }

        public static string? ValidarArchivo(IFormFile? archivo)
        {
            if (archivo is null)
            {
                return null;
            }
            if (archivo.Length == 0)
            {
                return "El archivo está vacío.";
            }
            if (archivo.Length > TamanioMaximo)
            {
                return "El archivo no puede pesar más de 5 MB.";
            }
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            if (!ExtensionesPermitidas.Contains(extension))
            {
                return "Solo se aceptan imágenes JPG o PNG y archivos PDF.";
            }
            return null;
        }
    }
}
