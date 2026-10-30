using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VuelvoSeguro.Data;
using VuelvoSeguro.Models;
using VuelvoSeguro.ViewModels;

namespace VuelvoSeguro.Controllers
{
    [Authorize(Roles = Rol.Pasajero)]
    public class PasajeroController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<Usuario> _userManager;

        public PasajeroController(ApplicationDbContext db, UserManager<Usuario> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var id = int.Parse(_userManager.GetUserId(User)!);
            var pasajero = await _db.Users
                .Include(u => u.Vehiculos)
                .FirstAsync(u => u.Id == id);
            return View(pasajero);
        }

        // RF03: el pasajero actualiza los datos de su vehículo.
        [HttpGet]
        public async Task<IActionResult> Vehiculo()
        {
            var vehiculo = await BuscarVehiculoAsync();
            if (vehiculo is null)
            {
                return View(new VehiculoViewModel());
            }
            return View(new VehiculoViewModel
            {
                Marca = vehiculo.Marca,
                Modelo = vehiculo.Modelo,
                Patente = vehiculo.Patente,
                EsAutomatico = vehiculo.EsAutomatico,
                VencimientoSeguro = vehiculo.VencimientoSeguro
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Vehiculo(VehiculoViewModel datos)
        {
            var vehiculo = await BuscarVehiculoAsync();
            var patente = VehiculoViewModel.NormalizarPatente(datos.Patente ?? string.Empty);
            if (await _db.Vehiculos.AnyAsync(v => v.Patente == patente && (vehiculo == null || v.Id != vehiculo.Id)))
            {
                ModelState.AddModelError(nameof(VehiculoViewModel.Patente), "Ya hay un vehículo registrado con esa patente.");
            }
            if (!ModelState.IsValid)
            {
                return View(datos);
            }

            if (vehiculo is null)
            {
                vehiculo = new Vehiculo { PropietarioId = int.Parse(_userManager.GetUserId(User)!) };
                _db.Vehiculos.Add(vehiculo);
            }
            vehiculo.Marca = datos.Marca;
            vehiculo.Modelo = datos.Modelo;
            vehiculo.Patente = patente;
            vehiculo.EsAutomatico = datos.EsAutomatico;
            vehiculo.VencimientoSeguro = datos.VencimientoSeguro!.Value;
            await _db.SaveChangesAsync();

            TempData["Mensaje"] = "Guardamos los datos de tu vehículo.";
            return RedirectToAction(nameof(Index));
        }

        private Task<Vehiculo?> BuscarVehiculoAsync()
        {
            var id = int.Parse(_userManager.GetUserId(User)!);
            return _db.Vehiculos.FirstOrDefaultAsync(v => v.PropietarioId == id);
        }
    }
}
