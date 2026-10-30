using System.ComponentModel.DataAnnotations;
using VuelvoSeguro.Validaciones;

namespace VuelvoSeguro.ViewModels
{
    // Datos que piden los dos registros (RF01).
    public abstract class RegistroViewModel
    {
        [Required(ErrorMessage = "Ingresá tu nombre.")]
        [StringLength(60)]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá tu apellido.")]
        [StringLength(60)]
        public string Apellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá tu DNI.")]
        [RegularExpression(@"^\d{7,8}$", ErrorMessage = "El DNI tiene que tener 7 u 8 números, sin puntos.")]
        [Display(Name = "DNI")]
        public string Dni { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá tu fecha de nacimiento.")]
        [DataType(DataType.Date)]
        [MayorDeEdad]
        [Display(Name = "Fecha de nacimiento")]
        public DateTime? FechaNacimiento { get; set; }

        [Required(ErrorMessage = "Ingresá tu correo.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá tu teléfono.")]
        [Phone(ErrorMessage = "El teléfono no es válido.")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresá una contraseña.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Repetí la contraseña.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
        [Display(Name = "Repetir contraseña")]
        public string ConfirmarPassword { get; set; } = string.Empty;
    }

    public class RegistroPasajeroViewModel : RegistroViewModel
    {
        [Required(ErrorMessage = "Ingresá la dirección de tu casa.")]
        [StringLength(150)]
        [Display(Name = "Dirección de tu casa")]
        public string Direccion { get; set; } = string.Empty;

        public VehiculoViewModel Vehiculo { get; set; } = new();
    }

    public class RegistroConductorViewModel : RegistroViewModel
    {
    }

    // Pantalla única de registro con el selector "Soy pasajero / Soy conductor", como en el prototipo.
    public class RegistroPaginaViewModel
    {
        public const string RolPasajero = "pasajero";
        public const string RolConductor = "conductor";

        public string Rol { get; set; } = RolPasajero;
        public RegistroPasajeroViewModel Pasajero { get; set; } = new();
        public RegistroConductorViewModel Conductor { get; set; } = new();
    }
}
