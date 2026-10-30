using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VuelvoSeguro.Data;
using VuelvoSeguro.Models;

namespace VuelvoSeguro.Controllers
{
    [Authorize(Roles = Rol.Administrador)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Conductores pendientes. Aprobar, rechazar, Viajes y Usuarios (RF04 y RF11) llegan en el sprint 3.
        public async Task<IActionResult> Index()
        {
            var pendientes = await _db.Users
                .Where(u => u.EstadoUsuarioId == EstadoUsuario.Pendiente)
                .OrderBy(u => u.Id)
                .ToListAsync();
            return View(pendientes);
        }
    }
}
