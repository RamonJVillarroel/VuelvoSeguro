namespace VuelvoSeguro.Servicios
{
    // Arma el motivo de cancelación a partir de lo elegido en la pantalla:
    // uno de los motivos sugeridos, o "Otro" con el texto que escribió el usuario.
    public static class CancelacionHelper
    {
        public const string Otro = "Otro";

        public static string? Motivo(string? elegido, string? otroMotivo)
        {
            if (string.IsNullOrWhiteSpace(elegido))
            {
                return null;
            }
            return elegido == Otro ? otroMotivo?.Trim() : elegido.Trim();
        }
    }
}
