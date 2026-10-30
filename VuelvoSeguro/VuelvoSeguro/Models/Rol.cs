using Microsoft.AspNetCore.Identity;

namespace VuelvoSeguro.Models
{
    // Roles del modelo de clases: Pasajero, Conductor y Administrador.
    public class Rol : IdentityRole<int>
    {
        public Rol() { }

        public Rol(string nombre) : base(nombre) { }

        public const string Pasajero = "Pasajero";
        public const string Conductor = "Conductor";
        public const string Administrador = "Administrador";
    }
}
