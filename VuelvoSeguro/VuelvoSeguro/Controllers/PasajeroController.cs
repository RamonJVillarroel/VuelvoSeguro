using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VuelvoSeguro.Data;
using VuelvoSeguro.Models;
using VuelvoSeguro.Servicios;
using VuelvoSeguro.ViewModels;

namespace VuelvoSeguro.Controllers
{
    [Authorize(Roles = Rol.Pasajero)]
    public class PasajeroController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<Usuario> _userManager;
        private readonly ViajeService _viajes;

        public PasajeroController(ApplicationDbContext db, UserManager<Usuario> userManager, ViajeService viajes)
        {
            _db = db;
            _userManager = userManager;
            _viajes = viajes;
        }

        // Inicio: botón "Volver a casa". Si hay un viaje activo, se muestra ese viaje.
        public async Task<IActionResult> Index()
        {
            var activo = await _viajes.ViajeActivoDelPasajeroAsync(UsuarioId());
            if (activo is not null)
            {
                return RedirectToAction(nameof(Viaje), new { id = activo.Id });
            }
            return View(await BuscarPasajeroAsync());
        }

        // RF05: publicar el viaje con un clic (el destino es siempre la casa del pasajero).
        [HttpPost]
        [ValidateAntiForgeryToken]
        // El origen llega como la ubicación del celular (latitud/longitud, con punto decimal) y,
        // si el pasajero no dio permiso de ubicación, como el texto que escribió.
        public async Task<IActionResult> Publicar(string? origen, string? latitud, string? longitud)
        {
            var resultado = await _viajes.PublicarAsync(UsuarioId(), origen, DateTime.Now, Coordenada(latitud), Coordenada(longitud));
            if (!resultado.Ok)
            {
                TempData["Error"] = resultado.Error;
                TempData["Origen"] = origen;
                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Viaje), new { id = resultado.ViajeId });
        }

        // RF08: el pasajero sigue el estado de su viaje.
        public async Task<IActionResult> Viaje(int id)
        {
            var viaje = await _viajes.BuscarAsync(id);
            if (viaje is null || viaje.PasajeroId != UsuarioId())
            {
                return NotFound();
            }
            return View(viaje);
        }

        // RF09: pantalla para cancelar el viaje eligiendo el motivo.
        [HttpGet]
        public async Task<IActionResult> Cancelar(int id)
        {
            var viaje = await _viajes.BuscarAsync(id);
            if (viaje is null || viaje.PasajeroId != UsuarioId())
            {
                return NotFound();
            }
            if (!ViajeService.EstadosCancelables.Contains(viaje.EstadoViajeId))
            {
                TempData["Error"] = viaje.EstadoViajeId == EstadoViaje.EnCurso
                    ? "Un viaje en curso no se puede cancelar."
                    : "Este viaje ya terminó.";
                return RedirectToAction(nameof(Viaje), new { id });
            }
            ViewData["Motivos"] = ViajeService.MotivosPasajero;
            return View("CancelarViaje", viaje);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id, string? motivo, string? otroMotivo)
        {
            var resultado = await _viajes.CancelarAsync(id, UsuarioId(), CancelacionHelper.Motivo(motivo, otroMotivo), DateTime.Now);
            if (!resultado.Ok)
            {
                TempData["Error"] = resultado.Error;
                // Si el viaje ya no se puede cancelar, la pantalla de cancelación lleva de vuelta al viaje.
                return RedirectToAction(nameof(Cancelar), new { id });
            }
            TempData["Mensaje"] = "Cancelaste el viaje.";
            return RedirectToAction(nameof(Viaje), new { id });
        }

        // RF10: historial de viajes del pasajero.
        public async Task<IActionResult> Historial()
        {
            return View(await _viajes.HistorialAsync(UsuarioId()));
        }

        public async Task<IActionResult> Perfil()
        {
            return View(await BuscarPasajeroAsync());
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

            TempData["Mensaje"] = "Guardamos los datos de tu auto.";
            return RedirectToAction(nameof(Perfil));
        }

        // Se parsea con cultura invariante: el navegador manda "-34.6037" y en es-AR el punto no es decimal.
        private static double? Coordenada(string? valor) =>
            double.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out var numero) ? numero : null;

        private int UsuarioId() => int.Parse(_userManager.GetUserId(User)!);

        private Task<Usuario> BuscarPasajeroAsync()
        {
            var id = int.Parse(_userManager.GetUserId(User)!);
            return _db.Users.Include(u => u.Vehiculos).FirstAsync(u => u.Id == id);
        }

        private Task<Vehiculo?> BuscarVehiculoAsync()
        {
            var id = int.Parse(_userManager.GetUserId(User)!);
            return _db.Vehiculos.FirstOrDefaultAsync(v => v.PropietarioId == id);
        }
    }
}
