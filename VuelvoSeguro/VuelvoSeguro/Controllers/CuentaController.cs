using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VuelvoSeguro.Data;
using VuelvoSeguro.Models;
using VuelvoSeguro.ViewModels;

namespace VuelvoSeguro.Controllers
{
    // RF01 (registro) y RF02 (inicio de sesión).
    public class CuentaController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly SignInManager<Usuario> _signInManager;
        private readonly ApplicationDbContext _db;

        public CuentaController(UserManager<Usuario> userManager, SignInManager<Usuario> signInManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _db = db;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel modelo)
        {
            if (!ModelState.IsValid)
            {
                return View(modelo);
            }

            var resultado = await _signInManager.PasswordSignInAsync(modelo.Email, modelo.Password, modelo.Recordarme, lockoutOnFailure: true);
            if (resultado.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "La cuenta quedó bloqueada por muchos intentos. Probá de nuevo en unos minutos.");
                return View(modelo);
            }
            if (!resultado.Succeeded)
            {
                ModelState.AddModelError(string.Empty, "El correo o la contraseña no son correctos.");
                return View(modelo);
            }

            if (!string.IsNullOrEmpty(modelo.ReturnUrl) && Url.IsLocalUrl(modelo.ReturnUrl))
            {
                return LocalRedirect(modelo.ReturnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Registro(string rol = RegistroPaginaViewModel.RolPasajero)
        {
            return View(new RegistroPaginaViewModel { Rol = rol });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistroPasajero([Bind(Prefix = "Pasajero")] RegistroPasajeroViewModel datos)
        {
            var patente = VehiculoViewModel.NormalizarPatente(datos.Vehiculo.Patente ?? string.Empty);
            if (await _db.Vehiculos.AnyAsync(v => v.Patente == patente))
            {
                ModelState.AddModelError("Pasajero.Vehiculo.Patente", "Ya hay un vehículo registrado con esa patente.");
            }
            await ValidarDniAsync(datos.Dni, "Pasajero");
            if (!ModelState.IsValid)
            {
                return View(nameof(Registro), new RegistroPaginaViewModel { Rol = RegistroPaginaViewModel.RolPasajero, Pasajero = datos });
            }

            var usuario = CrearUsuario(datos, EstadoUsuario.Activo);
            usuario.Direccion = datos.Direccion;

            // Usuario y vehículo se guardan juntos: si algo falla, no queda nada a medias.
            await using var transaccion = await _db.Database.BeginTransactionAsync();
            if (!await CrearCuentaAsync(usuario, datos.Password, Rol.Pasajero))
            {
                return View(nameof(Registro), new RegistroPaginaViewModel { Rol = RegistroPaginaViewModel.RolPasajero, Pasajero = datos });
            }

            _db.Vehiculos.Add(new Vehiculo
            {
                Marca = datos.Vehiculo.Marca,
                Modelo = datos.Vehiculo.Modelo,
                Patente = patente,
                EsAutomatico = datos.Vehiculo.EsAutomatico,
                VencimientoSeguro = datos.Vehiculo.VencimientoSeguro!.Value,
                PropietarioId = usuario.Id
            });
            await _db.SaveChangesAsync();
            await transaccion.CommitAsync();

            await _signInManager.SignInAsync(usuario, isPersistent: false);
            return RedirectToAction("Index", "Pasajero");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistroConductor([Bind(Prefix = "Conductor")] RegistroConductorViewModel datos)
        {
            await ValidarDniAsync(datos.Dni, "Conductor");
            if (!ModelState.IsValid)
            {
                return View(nameof(Registro), new RegistroPaginaViewModel { Rol = RegistroPaginaViewModel.RolConductor, Conductor = datos });
            }

            // El conductor queda Pendiente hasta que el administrador lo apruebe.
            var usuario = CrearUsuario(datos, EstadoUsuario.Pendiente);
            if (!await CrearCuentaAsync(usuario, datos.Password, Rol.Conductor))
            {
                return View(nameof(Registro), new RegistroPaginaViewModel { Rol = RegistroPaginaViewModel.RolConductor, Conductor = datos });
            }

            await _signInManager.SignInAsync(usuario, isPersistent: false);
            return RedirectToAction("Index", "Conductor");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            return View();
        }

        private static Usuario CrearUsuario(RegistroViewModel modelo, int estadoUsuarioId)
        {
            return new Usuario
            {
                UserName = modelo.Email,
                Email = modelo.Email,
                PhoneNumber = modelo.Telefono,
                Nombre = modelo.Nombre,
                Apellido = modelo.Apellido,
                Dni = modelo.Dni,
                FechaNacimiento = modelo.FechaNacimiento!.Value,
                EstadoUsuarioId = estadoUsuarioId
            };
        }

        private async Task ValidarDniAsync(string dni, string prefijo)
        {
            if (!string.IsNullOrEmpty(dni) && await _userManager.Users.AnyAsync(u => u.Dni == dni))
            {
                ModelState.AddModelError($"{prefijo}.{nameof(RegistroViewModel.Dni)}", "Ya existe una cuenta con ese DNI.");
            }
        }

        private async Task<bool> CrearCuentaAsync(Usuario usuario, string password, string rol)
        {
            var resultado = await _userManager.CreateAsync(usuario, password);
            if (resultado.Succeeded)
            {
                resultado = await _userManager.AddToRoleAsync(usuario, rol);
            }
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return resultado.Succeeded;
        }
    }
}
