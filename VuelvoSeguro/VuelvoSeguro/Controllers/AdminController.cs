using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VuelvoSeguro.Models;

namespace VuelvoSeguro.Controllers
{
    [Authorize(Roles = Rol.Administrador)]
    public class AdminController : Controller
    {
        // El panel completo (RF04 y RF11) llega en el sprint 3.
        public IActionResult Index()
        {
            return View();
        }
    }
}
