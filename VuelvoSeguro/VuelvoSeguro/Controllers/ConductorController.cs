using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VuelvoSeguro.Data;
using VuelvoSeguro.Models;

namespace VuelvoSeguro.Controllers
{
    [Authorize(Roles = Rol.Conductor)]
    public class ConductorController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<Usuario> _userManager;

        public ConductorController(ApplicationDbContext db, UserManager<Usuario> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var id = int.Parse(_userManager.GetUserId(User)!);
            var conductor = await _db.Users
                .Include(u => u.EstadoUsuario)
                .FirstAsync(u => u.Id == id);
            return View(conductor);
        }
    }
}
