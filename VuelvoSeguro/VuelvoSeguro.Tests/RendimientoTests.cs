using System.Diagnostics;
using VuelvoSeguro.Models;
using VuelvoSeguro.Servicios;

namespace VuelvoSeguro.Tests
{
    // RNF01 (rendimiento): las consultas que más se usan responden rápido aun con muchos viajes cargados.
    // El límite es holgado (1 segundo) para que la prueba no dependa de la máquina.
    public class RendimientoTests : IDisposable
    {
        private const int CantidadViajes = 1000;
        private static readonly TimeSpan Limite = TimeSpan.FromSeconds(1);

        private readonly BaseDeDatosDePrueba _base = new();

        public void Dispose() => _base.Dispose();

        private (int Pasajero, int Conductor) CargarViajes()
        {
            var pasajero = _base.CrearPasajero();
            var conductor = _base.CrearConductor();
            using var db = _base.CrearContexto();
            var vehiculo = db.Vehiculos.First(v => v.PropietarioId == pasajero);
            var inicio = DateTime.Now.AddDays(-CantidadViajes);
            for (var i = 0; i < CantidadViajes; i++)
            {
                // Mitad finalizados con el conductor, un cuarto cancelados y un cuarto publicados esperando conductor.
                var estado = (i % 4) switch { 0 or 1 => EstadoViaje.Finalizado, 2 => EstadoViaje.Cancelado, _ => EstadoViaje.Publicado };
                db.Viajes.Add(new Viaje
                {
                    PasajeroId = pasajero,
                    VehiculoId = vehiculo.Id,
                    ConductorId = estado == EstadoViaje.Finalizado ? conductor : null,
                    DireccionOrigen = "Origen " + i,
                    DireccionDestino = "Av. Rivadavia 4520",
                    FechaPublicacion = inicio.AddDays(i),
                    EstadoViajeId = estado,
                    MotivoCancelacion = estado == EstadoViaje.Cancelado ? "Me quedo un rato más" : null,
                    CanceladoPorId = estado == EstadoViaje.Cancelado ? pasajero : null
                });
            }
            db.SaveChanges();
            return (pasajero, conductor);
        }

        private static async Task<(T Resultado, TimeSpan Tiempo)> MedirAsync<T>(Func<Task<T>> consulta)
        {
            await consulta(); // primera ejecución: compila la consulta
            var reloj = Stopwatch.StartNew();
            var resultado = await consulta();
            reloj.Stop();
            return (resultado, reloj.Elapsed);
        }

        [Fact]
        public async Task Disponibles_con1000Viajes_respondeEnMenosDeUnSegundo()
        {
            CargarViajes();
            var servicio = new ViajeService(_base.CrearContexto());

            var (lista, tiempo) = await MedirAsync(() => servicio.DisponiblesAsync());

            Assert.Equal(CantidadViajes / 4, lista.Count);
            Assert.True(tiempo < Limite, $"Tardó {tiempo.TotalMilliseconds:0} ms");
        }

        [Fact]
        public async Task Historial_con1000Viajes_respondeEnMenosDeUnSegundo()
        {
            var (pasajero, conductor) = CargarViajes();
            var servicio = new ViajeService(_base.CrearContexto());

            var (delPasajero, tiempoPasajero) = await MedirAsync(() => servicio.HistorialAsync(pasajero));
            var (delConductor, tiempoConductor) = await MedirAsync(() => servicio.HistorialAsync(conductor));

            Assert.Equal(CantidadViajes * 3 / 4, delPasajero.Count);
            Assert.Equal(CantidadViajes / 2, delConductor.Count);
            Assert.True(tiempoPasajero < Limite, $"Tardó {tiempoPasajero.TotalMilliseconds:0} ms");
            Assert.True(tiempoConductor < Limite, $"Tardó {tiempoConductor.TotalMilliseconds:0} ms");
        }
    }
}
