using System.ComponentModel.DataAnnotations;

namespace VuelvoSeguro.Validaciones
{
    // RF01: rechaza a menores de 18 años.
    public class MayorDeEdadAttribute : ValidationAttribute
    {
        public const int EdadMinima = 18;

        public MayorDeEdadAttribute()
            : base($"Tenés que ser mayor de {EdadMinima} años para registrarte.")
        {
        }

        public override bool IsValid(object? value)
        {
            if (value is not DateTime fechaNacimiento)
            {
                return true; // [Required] se encarga del campo vacío.
            }
            return EsMayorDeEdad(fechaNacimiento, DateTime.Today);
        }

        public static bool EsMayorDeEdad(DateTime fechaNacimiento, DateTime hoy)
        {
            return fechaNacimiento.Date <= hoy.Date.AddYears(-EdadMinima);
        }
    }
}
