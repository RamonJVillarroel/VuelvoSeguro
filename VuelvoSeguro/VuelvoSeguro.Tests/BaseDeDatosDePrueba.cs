using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VuelvoSeguro.Data;
using VuelvoSeguro.Models;

namespace VuelvoSeguro.Tests
{
    // Base SQLite en memoria: se crea limpia para cada prueba y se descarta al terminar.
    public sealed class BaseDeDatosDePrueba : IDisposable
    {
        private readonly SqliteConnection _conexion;
        private int _siguienteDni = 30000000;

        public BaseDeDatosDePrueba()
        {
            _conexion = new SqliteConnection("DataSource=:memory:");
            _conexion.Open();
            using var db = CrearContexto();
            db.Database.EnsureCreated();
        }

        public ApplicationDbContext CrearContexto() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(_conexion).Options);

        public int CrearPasajero(bool seguroVigente = true, string? direccion = "Av. Rivadavia 4520")
        {
            using var db = CrearContexto();
            var pasajero = NuevoUsuario("Lucía", "Gómez", EstadoUsuario.Activo);
            pasajero.Direccion = direccion;
            pasajero.Vehiculos.Add(new Vehiculo
            {
                Marca = "Volkswagen",
                Modelo = "Gol Trend",
                Patente = "AB" + _siguienteDni % 1000 + "CD",
                VencimientoSeguro = seguroVigente ? DateTime.Today.AddYears(1) : DateTime.Today.AddDays(-1)
            });
            db.Users.Add(pasajero);
            db.SaveChanges();
            return pasajero.Id;
        }

        public int CrearConductor(int estado = EstadoUsuario.Aprobado)
        {
            using var db = CrearContexto();
            var conductor = NuevoUsuario("Martín", "Pérez", estado);
            db.Users.Add(conductor);
            db.SaveChanges();
            return conductor.Id;
        }

        private Usuario NuevoUsuario(string nombre, string apellido, int estado)
        {
            var dni = (_siguienteDni++).ToString();
            return new Usuario
            {
                UserName = $"{dni}@mail.com",
                Email = $"{dni}@mail.com",
                Nombre = nombre,
                Apellido = apellido,
                Dni = dni,
                FechaNacimiento = new DateTime(1990, 1, 1),
                EstadoUsuarioId = estado
            };
        }

        public void Dispose() => _conexion.Dispose();
    }
}
