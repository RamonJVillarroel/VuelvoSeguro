using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VuelvoSeguro.Models;

namespace VuelvoSeguro.Data
{
    // Al iniciar la app: aplica las migraciones y crea los roles y el administrador.
    public static class DatosIniciales
    {
        public static async Task CargarAsync(IServiceProvider servicios)
        {
            using var scope = servicios.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Rol>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            await db.Database.MigrateAsync();

            foreach (var nombre in new[] { Rol.Pasajero, Rol.Conductor, Rol.Administrador })
            {
                if (!await roleManager.RoleExistsAsync(nombre))
                {
                    await roleManager.CreateAsync(new Rol(nombre));
                }
            }

            var email = config["AdminInicial:Email"];
            var password = config["AdminInicial:Password"];
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                return;
            }

            if (await userManager.FindByEmailAsync(email) is null)
            {
                var admin = new Usuario
                {
                    UserName = email,
                    Email = email,
                    Nombre = "Admin",
                    Apellido = "VuelvoSeguro",
                    Dni = "00000000",
                    FechaNacimiento = new DateTime(1990, 1, 1),
                    EstadoUsuarioId = EstadoUsuario.Activo
                };
                var resultado = await userManager.CreateAsync(admin, password);
                if (resultado.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, Rol.Administrador);
                }
            }
        }
    }
}
