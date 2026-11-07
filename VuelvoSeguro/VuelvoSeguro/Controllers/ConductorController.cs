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

        public ConductorController(ApplicationDbContext db, UserManager<Usuario> userManager, AlmacenDocumentos documentos)
        {
            _db = db;
            _userManager = userManager;
            _documentos = documentos;
        }

        // Un conductor sin aprobar solo ve la pantalla de espera (con el envío de documentos).
        public async Task<IActionResult> Index()
        {
            var conductor = await BuscarConductorAsync();
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

        private Task<Usuario> BuscarConductorAsync()
        {
            var id = int.Parse(_userManager.GetUserId(User)!);
            return _db.Users.Include(u => u.EstadoUsuario).FirstAsync(u => u.Id == id);
        }
    }
}
