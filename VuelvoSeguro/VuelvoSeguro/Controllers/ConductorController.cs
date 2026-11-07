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
    [Authorize(Roles = Rol.Conductor)]
    public class ConductorController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<Usuario> _userManager;
        private readonly AlmacenDocumentos _documentos;
        private readonly ViajeService _viajes;

        public ConductorController(ApplicationDbContext db, UserManager<Usuario> userManager, AlmacenDocumentos documentos, ViajeService viajes)
        {
            _db = db;
            _userManager = userManager;
            _documentos = documentos;
            _viajes = viajes;
        }

        // Un conductor sin aprobar solo ve la pantalla de espera (con el envío de documentos).
        public async Task<IActionResult> Index()
        {
            var conductor = await BuscarConductorAsync();
            if (conductor.EstadoUsuarioId == EstadoUsuario.Aprobado)
            {
                return RedirectToAction(nameof(Disponibles));
            }
            ViewData["Documentacion"] = new DocumentacionViewModel
            {
                NumeroLicencia = conductor.NumeroLicencia ?? string.Empty,
                VencimientoLicencia = conductor.VencimientoLicencia
            };
            return View(conductor);
        }

        // RF04: el conductor envía licencia, DNI y certificado de antecedentes penales.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> Documentacion(DocumentacionViewModel datos)
        {
            var conductor = await BuscarConductorAsync();
            if (conductor.EstadoUsuarioId != EstadoUsuario.Pendiente)
            {
                return RedirectToAction(nameof(Index));
            }
            if (!ModelState.IsValid)
            {
                ViewData["Documentacion"] = datos;
                return View(nameof(Index), conductor);
            }

            conductor.NumeroLicencia = datos.NumeroLicencia.Trim();
            conductor.VencimientoLicencia = datos.VencimientoLicencia!.Value.Date;
            conductor.RutaLicencia = await _documentos.GuardarAsync(conductor.Id, "licencia", datos.FotoLicencia!);
            conductor.RutaDni = await _documentos.GuardarAsync(conductor.Id, "dni", datos.FotoDni!);
            conductor.RutaAntecedentes = await _documentos.GuardarAsync(conductor.Id, "antecedentes", datos.Antecedentes!);
            await _db.SaveChangesAsync();

            TempData["Mensaje"] = "Recibimos tu documentación. Un administrador la va a revisar.";
            return RedirectToAction(nameof(Index));
        }

        // RF07: viajes publicados que esperan un conductor.
        public async Task<IActionResult> Disponibles()
        {
            if (!await EstaAprobadoAsync())
            {
                return RedirectToAction(nameof(Index));
            }
            ViewData["ViajeActivo"] = await _viajes.ViajeActivoDelConductorAsync(UsuarioId());
            return View(await _viajes.DisponiblesAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Aceptar(int id)
        {
            var resultado = await _viajes.AceptarAsync(id, UsuarioId());
            if (!resultado.Ok)
            {
                TempData["Error"] = resultado.Error;
                return RedirectToAction(nameof(Disponibles));
            }
            return RedirectToAction(nameof(Viaje), new { id });
        }

        // RF08: el conductor ve su viaje y avanza el estado.
        public async Task<IActionResult> Viaje(int id)
        {
            var viaje = await _viajes.BuscarAsync(id);
            if (viaje is null || viaje.ConductorId != UsuarioId())
            {
                return NotFound();
            }
            return View(viaje);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Iniciar(int id)
        {
            var resultado = await _viajes.IniciarAsync(id, UsuarioId(), DateTime.Now);
            if (!resultado.Ok)
            {
                TempData["Error"] = resultado.Error;
            }
            return RedirectToAction(nameof(Viaje), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finalizar(int id)
        {
            var resultado = await _viajes.FinalizarAsync(id, UsuarioId(), DateTime.Now);
            if (!resultado.Ok)
            {
                TempData["Error"] = resultado.Error;
                return RedirectToAction(nameof(Viaje), new { id });
            }
            TempData["Mensaje"] = "¡Viaje finalizado! El pasajero llegó a su casa.";
            return RedirectToAction(nameof(Disponibles));
        }

        public async Task<IActionResult> Perfil()
        {
            return View(await BuscarConductorAsync());
        }

        private int UsuarioId() => int.Parse(_userManager.GetUserId(User)!);

        private Task<bool> EstaAprobadoAsync()
        {
            var id = UsuarioId();
            return _db.Users.AnyAsync(u => u.Id == id && u.EstadoUsuarioId == EstadoUsuario.Aprobado);
        }

        private Task<Usuario> BuscarConductorAsync()
        {
            var id = UsuarioId();
            return _db.Users.Include(u => u.EstadoUsuario).FirstAsync(u => u.Id == id);
        }
    }
}
