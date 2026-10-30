using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VuelvoSeguro.Models;

namespace VuelvoSeguro.Controllers
{
    public class HomeController : Controller
    {
        // RF02: cada rol entra directo a sus pantallas.
        public IActionResult Index()
        {
            if (User.IsInRole(Rol.Administrador))
            {
                return RedirectToAction("Index", "Admin");
            }
            if (User.IsInRole(Rol.Conductor))
            {
                return RedirectToAction("Index", "Conductor");
            }
            if (User.IsInRole(Rol.Pasajero))
            {
                return RedirectToAction("Index", "Pasajero");
            }
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
