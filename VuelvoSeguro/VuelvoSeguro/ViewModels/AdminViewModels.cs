using VuelvoSeguro.Models;

namespace VuelvoSeguro.ViewModels
{
    public class ConductoresPendientesViewModel
    {
        public List<Usuario> Pendientes { get; set; } = new();
        public Usuario? Seleccionado { get; set; }
    }

    public class UsuarioAdminViewModel
    {
        public Usuario Usuario { get; set; } = null!;
        public string Rol { get; set; } = string.Empty;
    }
}
