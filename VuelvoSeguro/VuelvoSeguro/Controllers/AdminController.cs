using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VuelvoSeguro.Data;
using VuelvoSeguro.Models;
using VuelvoSeguro.Servicios;
using VuelvoSeguro.ViewModels;

namespace VuelvoSeguro.Controllers
{
    // RF04 (validación de conductores) y RF11 (panel de administración).
    [Authorize(Roles = Rol.Administrador)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly AlmacenDocumentos _documentos;

        public AdminController(ApplicationDbContext db, AlmacenDocumentos documentos)
        {
            _db = db;
            _documentos = documentos;
        }

        // Conductores pendientes: lista a la izquierda y detalle del seleccionado a la derecha.
        public async Task<IActionResult> Index(int? id)
        {
            var pendientes = await _db.Users
                .Where(u => u.EstadoUsuarioId == EstadoUsuario.Pendiente
                    && _db.UserRoles.Any(ur => ur.UserId == u.Id && _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == Rol.Conductor)))
                .OrderBy(u => u.Id)
                .ToListAsync();
            var seleccionado = pendientes.FirstOrDefault(u => u.Id == id) ?? pendientes.FirstOrDefault();
            ViewData["Pendientes"] = pendientes.Count;
            return View(new ConductoresPendientesViewModel { Pendientes = pendientes, Seleccionado = seleccionado });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Aprobar(int id) => ResolverAsync(id, aprobar: true);

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Rechazar(int id) => ResolverAsync(id, aprobar: false);

        // Muestra un documento del conductor (licencia, dni o antecedentes).
        public async Task<IActionResult> Documento(int id, string tipo)
        {
            var conductor = await _db.Users.FindAsync(id);
            if (conductor is null)
            {
                return NotFound();
            }
            var ruta = tipo switch
            {
                "licencia" => conductor.RutaLicencia,
                "dni" => conductor.RutaDni,
                "antecedentes" => conductor.RutaAntecedentes,
                _ => null
            };
            var archivo = _documentos.Buscar(ruta);
            if (archivo is null)
            {
                return NotFound();
            }
            return PhysicalFile(archivo.Value.RutaCompleta, archivo.Value.TipoContenido);
        }

        public async Task<IActionResult> Viajes()
        {
            var viajes = await _db.Viajes
                .Include(v => v.Pasajero)
                .Include(v => v.Conductor)
                .Include(v => v.EstadoViaje)
                .OrderByDescending(v => v.FechaPublicacion)
                .ToListAsync();
            await ContarPendientesAsync();
            return View(viajes);
        }

        public async Task<IActionResult> Usuarios()
        {
            var usuarios = await _db.Users
                .Include(u => u.EstadoUsuario)
                .OrderBy(u => u.Apellido).ThenBy(u => u.Nombre)
                .Select(u => new UsuarioAdminViewModel
                {
                    Usuario = u,
                    Rol = _db.UserRoles.Where(ur => ur.UserId == u.Id)
                        .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name!)
                        .FirstOrDefault() ?? "-"
                })
                .ToListAsync();
            await ContarPendientesAsync();
            return View(usuarios);
        }

        private async Task<IActionResult> ResolverAsync(int id, bool aprobar)
        {
            var conductor = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.EstadoUsuarioId == EstadoUsuario.Pendiente);
            if (conductor is null)
            {
                return NotFound();
            }
            if (aprobar && conductor.RutaAntecedentes is null)
            {
                TempData["Error"] = "No se puede aprobar a un conductor que no envió su documentación.";
                return RedirectToAction(nameof(Index), new { id });
            }

            conductor.EstadoUsuarioId = aprobar ? EstadoUsuario.Aprobado : EstadoUsuario.Rechazado;
            conductor.FechaAprobacion = aprobar ? DateTime.Now : null;
            await _db.SaveChangesAsync();

            TempData["Mensaje"] = $"{conductor.NombreCompleto} quedó {(aprobar ? "aprobado" : "rechazado")}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ContarPendientesAsync()
        {
            ViewData["Pendientes"] = await _db.Users.CountAsync(u => u.EstadoUsuarioId == EstadoUsuario.Pendiente);
        }
    }
}
