using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VuelvoSeguro.Models;

namespace VuelvoSeguro.Data
{
    public class ApplicationDbContext : IdentityDbContext<Usuario, Rol, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Vehiculo> Vehiculos => Set<Vehiculo>();
        public DbSet<Viaje> Viajes => Set<Viaje>();
        public DbSet<EstadoUsuario> EstadosUsuario => Set<EstadoUsuario>();
        public DbSet<EstadoViaje> EstadosViaje => Set<EstadoViaje>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Mismos nombres de tabla que el modelo de clases.
            builder.Entity<Usuario>().ToTable("Users");
            builder.Entity<Rol>().ToTable("Roles");
            builder.Entity<EstadoUsuario>().ToTable("EstadoUsuario");
            builder.Entity<EstadoViaje>().ToTable("EstadoViaje");
            builder.Entity<Vehiculo>().ToTable("Vehiculo");
            builder.Entity<Viaje>().ToTable("Viajes");

            builder.Entity<Usuario>(u =>
            {
                u.HasIndex(x => x.Dni).IsUnique();
                u.Property(x => x.FechaNacimiento).HasColumnType("date");
                u.Property(x => x.VencimientoLicencia).HasColumnType("date");
                u.HasOne(x => x.EstadoUsuario)
                    .WithMany(e => e.Usuarios)
                    .HasForeignKey(x => x.EstadoUsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Vehiculo>(v =>
            {
                v.HasIndex(x => x.Patente).IsUnique();
                v.Property(x => x.VencimientoSeguro).HasColumnType("date");
                v.HasOne(x => x.Propietario)
                    .WithMany(u => u.Vehiculos)
                    .HasForeignKey(x => x.PropietarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Viaje>(v =>
            {
                v.HasOne(x => x.Pasajero)
                    .WithMany(u => u.ViajesComoPasajero)
                    .HasForeignKey(x => x.PasajeroId)
                    .OnDelete(DeleteBehavior.Restrict);
                v.HasOne(x => x.Conductor)
                    .WithMany(u => u.ViajesComoConductor)
                    .HasForeignKey(x => x.ConductorId)
                    .OnDelete(DeleteBehavior.Restrict);
                v.HasOne(x => x.Vehiculo)
                    .WithMany()
                    .HasForeignKey(x => x.VehiculoId)
                    .OnDelete(DeleteBehavior.Restrict);
                v.HasOne(x => x.EstadoViaje)
                    .WithMany(e => e.Viajes)
                    .HasForeignKey(x => x.EstadoViajeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<EstadoUsuario>().HasData(
                new EstadoUsuario { Id = EstadoUsuario.Activo, NombreEstado = "Activo" },
                new EstadoUsuario { Id = EstadoUsuario.Pendiente, NombreEstado = "Pendiente" },
                new EstadoUsuario { Id = EstadoUsuario.Aprobado, NombreEstado = "Aprobado" },
                new EstadoUsuario { Id = EstadoUsuario.Rechazado, NombreEstado = "Rechazado" },
                new EstadoUsuario { Id = EstadoUsuario.Suspendido, NombreEstado = "Suspendido" });

            builder.Entity<EstadoViaje>().HasData(
                new EstadoViaje { Id = EstadoViaje.Publicado, NombreEstado = "Publicado" },
                new EstadoViaje { Id = EstadoViaje.Aceptado, NombreEstado = "Aceptado" },
                new EstadoViaje { Id = EstadoViaje.EnCurso, NombreEstado = "En curso" },
                new EstadoViaje { Id = EstadoViaje.Finalizado, NombreEstado = "Finalizado" },
                new EstadoViaje { Id = EstadoViaje.Cancelado, NombreEstado = "Cancelado" });
        }
    }
}
