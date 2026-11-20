using System.ComponentModel.DataAnnotations;

namespace VuelvoSeguro.Validaciones
{
    // RNF08: una casilla que tiene que estar marcada (por ejemplo, aceptar los términos y condiciones).
    public class DebeAceptarseAttribute : ValidationAttribute
    {
        public DebeAceptarseAttribute()
            : base("Tenés que aceptar los términos y condiciones para crear la cuenta.")
        {
        }

        public override bool IsValid(object? value) => value is true;
    }
}
