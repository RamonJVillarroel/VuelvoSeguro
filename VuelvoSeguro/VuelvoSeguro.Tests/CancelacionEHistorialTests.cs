using VuelvoSeguro.Models;
using VuelvoSeguro.Servicios;

namespace VuelvoSeguro.Tests
{
    // Pruebas del sprint 5: RF09 (cancelar con motivo) y RF10 (historial de viajes).
    public class CancelacionEHistorialTests : IDisposable
    {
        private const string Motivo = "Ya conseguí otra forma de volver";

        private readonly BaseDeDatosDePrueba _base = new();
        private readonly DateTime _ahora = DateTime.Now;

        public void Dispose() => _base.Dispose();

        private ViajeService Servicio() => new(_base.CrearContexto());

        private async Task<int> PublicarAsync(int pasajeroId)
        {
            var resultado = await Servicio().PublicarAsync(pasajeroId, "Bar La Esquina, Av. Corrientes 1800", _ahora);
            Assert.True(resultado.Ok, resultado.Error);
            return resultado.ViajeId!.Value;
        }

        private async Task<int> AceptadoAsync(int pasajeroId, int conductorId)
        {
            var id = await PublicarAsync(pasajeroId);
            Assert.True((await Servicio().AceptarAsync(id, conductorId)).Ok);
            return id;
        }

        private async Task<int> EnCursoAsync(int pasajeroId, int conductorId)
        {
            var id = await AceptadoAsync(pasajeroId, conductorId);
            Assert.True((await Servicio().IniciarAsync(id, conductorId, _ahora)).Ok);
            return id;
        }

        // ---------- RF09: cancelación ----------

        [Fact]
        public async Task Cancelar_pasajeroViajePublicado_quedaCanceladoConMotivo()
        {
            var pasajero = _base.CrearPasajero();
            var id = await PublicarAsync(pasajero);

            var resultado = await Servicio().CancelarAsync(id, pasajero, Motivo, _ahora);

            Assert.True(resultado.Ok, resultado.Error);
            var viaje = await Servicio().BuscarAsync(id);
            Assert.Equal(EstadoViaje.Cancelado, viaje!.EstadoViajeId);
            Assert.Equal(Motivo, viaje.MotivoCancelacion);
            Assert.Equal(pasajero, viaje.CanceladoPorId);
            Assert.NotNull(viaje.FechaCancelacion);
        }

        [Fact]
        public async Task Cancelar_pasajeroViajeAceptado_seCancelaYLiberaAlConductor()
        {
            var pasajero = _base.CrearPasajero();
            var conductor = _base.CrearConductor();
            var id = await AceptadoAsync(pasajero, conductor);

            var resultado = await Servicio().CancelarAsync(id, pasajero, Motivo, _ahora);

            Assert.True(resultado.Ok, resultado.Error);
            Assert.Null(await Servicio().ViajeActivoDelConductorAsync(conductor));
        }

        [Fact]
        public async Task Cancelar_conductorViajeAceptado_seCancelaYQuedaRegistradoQuienLoHizo()
        {
            var pasajero = _base.CrearPasajero();
            var conductor = _base.CrearConductor();
            var id = await AceptadoAsync(pasajero, conductor);

            var resultado = await Servicio().CancelarAsync(id, conductor, "Tuve un imprevisto", _ahora);

            Assert.True(resultado.Ok, resultado.Error);
            var viaje = await Servicio().BuscarAsync(id);
            Assert.Equal(EstadoViaje.Cancelado, viaje!.EstadoViajeId);
            Assert.Equal(conductor, viaje.CanceladoPorId);
            Assert.Equal(conductor, viaje.ConductorId); // sigue apareciendo en el historial del conductor
        }

        [Fact]
        public async Task Cancelar_viajeEnCurso_seRechaza()
        {
            var pasajero = _base.CrearPasajero();
            var conductor = _base.CrearConductor();
            var id = await EnCursoAsync(pasajero, conductor);

            var porPasajero = await Servicio().CancelarAsync(id, pasajero, Motivo, _ahora);
            var porConductor = await Servicio().CancelarAsync(id, conductor, "Tuve un imprevisto", _ahora);

            Assert.False(porPasajero.Ok);
            Assert.False(porConductor.Ok);
            Assert.Equal("Un viaje en curso no se puede cancelar.", porPasajero.Error);
            Assert.Equal(EstadoViaje.EnCurso, (await Servicio().BuscarAsync(id))!.EstadoViajeId);
        }

        [Fact]
        public async Task Cancelar_viajeFinalizado_seRechaza()
        {
            var pasajero = _base.CrearPasajero();
            var conductor = _base.CrearConductor();
            var id = await EnCursoAsync(pasajero, conductor);
            Assert.True((await Servicio().FinalizarAsync(id, conductor, _ahora)).Ok);

            var resultado = await Servicio().CancelarAsync(id, pasajero, Motivo, _ahora);

            Assert.False(resultado.Ok);
            Assert.Equal(EstadoViaje.Finalizado, (await Servicio().BuscarAsync(id))!.EstadoViajeId);
        }

        [Fact]
        public async Task Cancelar_dosVeces_laSegundaSeRechaza()
        {
            var pasajero = _base.CrearPasajero();
            var id = await PublicarAsync(pasajero);
            Assert.True((await Servicio().CancelarAsync(id, pasajero, Motivo, _ahora)).Ok);

            var segunda = await Servicio().CancelarAsync(id, pasajero, "Otro motivo", _ahora);

            Assert.False(segunda.Ok);
            Assert.Equal(Motivo, (await Servicio().BuscarAsync(id))!.MotivoCancelacion);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("    ")]
        public async Task Cancelar_sinMotivo_seRechaza(string? motivo)
        {
            var pasajero = _base.CrearPasajero();
            var id = await PublicarAsync(pasajero);

            var resultado = await Servicio().CancelarAsync(id, pasajero, motivo, _ahora);

            Assert.False(resultado.Ok);
            Assert.Equal(EstadoViaje.Publicado, (await Servicio().BuscarAsync(id))!.EstadoViajeId);
        }

        [Fact]
        public async Task Cancelar_motivoDemasiadoLargo_seRechaza()
        {
            var pasajero = _base.CrearPasajero();
            var id = await PublicarAsync(pasajero);

            var resultado = await Servicio().CancelarAsync(id, pasajero, new string('a', ViajeService.LargoMaximoMotivo + 1), _ahora);

            Assert.False(resultado.Ok);
        }

        [Fact]
        public async Task Cancelar_viajeDeOtraPersona_seRechaza()
        {
            var pasajero = _base.CrearPasajero();
            var otroPasajero = _base.CrearPasajero();
            var otroConductor = _base.CrearConductor();
            var id = await PublicarAsync(pasajero);

            Assert.False((await Servicio().CancelarAsync(id, otroPasajero, Motivo, _ahora)).Ok);
            Assert.False((await Servicio().CancelarAsync(id, otroConductor, Motivo, _ahora)).Ok);
            Assert.Equal(EstadoViaje.Publicado, (await Servicio().BuscarAsync(id))!.EstadoViajeId);
        }

        [Fact]
        public async Task Cancelar_permitePublicarOtroViaje()
        {
            var pasajero = _base.CrearPasajero();
            var id = await PublicarAsync(pasajero);
            Assert.True((await Servicio().CancelarAsync(id, pasajero, Motivo, _ahora)).Ok);

            var nuevo = await Servicio().PublicarAsync(pasajero, "Av. Santa Fe 3200", _ahora);

            Assert.True(nuevo.Ok, nuevo.Error);
        }

        [Fact]
        public async Task Aceptar_viajeCancelado_seRechaza()
        {
            var pasajero = _base.CrearPasajero();
            var conductor = _base.CrearConductor();
            var id = await PublicarAsync(pasajero);
            Assert.True((await Servicio().CancelarAsync(id, pasajero, Motivo, _ahora)).Ok);

            var resultado = await Servicio().AceptarAsync(id, conductor);

            Assert.False(resultado.Ok);
            Assert.DoesNotContain(await Servicio().DisponiblesAsync(), v => v.Id == id);
        }

        [Fact]
        public async Task Iniciar_viajeCanceladoPorElPasajero_seRechaza()
        {
            var pasajero = _base.CrearPasajero();
            var conductor = _base.CrearConductor();
            var id = await AceptadoAsync(pasajero, conductor);
            Assert.True((await Servicio().CancelarAsync(id, pasajero, Motivo, _ahora)).Ok);

            var resultado = await Servicio().IniciarAsync(id, conductor, _ahora);

            Assert.False(resultado.Ok);
        }

        // ---------- RF10: historial ----------

        [Fact]
        public async Task Historial_sinViajes_estaVacio()
        {
            var pasajero = _base.CrearPasajero();

            Assert.Empty(await Servicio().HistorialAsync(pasajero));
        }

        [Fact]
        public async Task Historial_muestraFinalizadosYCancelados_noLosActivos()
        {
            var pasajero = _base.CrearPasajero();
            var conductor = _base.CrearConductor();
            var finalizado = await EnCursoAsync(pasajero, conductor);
            Assert.True((await Servicio().FinalizarAsync(finalizado, conductor, _ahora)).Ok);
            var cancelado = await PublicarAsync(pasajero);
            Assert.True((await Servicio().CancelarAsync(cancelado, pasajero, Motivo, _ahora)).Ok);
            var activo = await PublicarAsync(pasajero);

            var historial = await Servicio().HistorialAsync(pasajero);

            Assert.Equal(2, historial.Count);
            Assert.Contains(historial, v => v.Id == finalizado);
            Assert.Contains(historial, v => v.Id == cancelado);
            Assert.DoesNotContain(historial, v => v.Id == activo);
        }

        [Fact]
        public async Task Historial_delConductor_soloSusViajes()
        {
            var pasajero = _base.CrearPasajero();
            var conductor = _base.CrearConductor();
            var otroConductor = _base.CrearConductor();
            var suyo = await EnCursoAsync(pasajero, conductor);
            Assert.True((await Servicio().FinalizarAsync(suyo, conductor, _ahora)).Ok);
            var deOtro = await EnCursoAsync(pasajero, otroConductor);
            Assert.True((await Servicio().FinalizarAsync(deOtro, otroConductor, _ahora)).Ok);

            var historial = await Servicio().HistorialAsync(conductor);

            var viaje = Assert.Single(historial);
            Assert.Equal(suyo, viaje.Id);
            Assert.NotNull(viaje.Pasajero);
            Assert.NotNull(viaje.EstadoViaje);
        }

        [Fact]
        public async Task Historial_noMezclaPasajeros()
        {
            var pasajero = _base.CrearPasajero();
            var otroPasajero = _base.CrearPasajero();
            var id = await PublicarAsync(otroPasajero);
            Assert.True((await Servicio().CancelarAsync(id, otroPasajero, Motivo, _ahora)).Ok);

            Assert.Empty(await Servicio().HistorialAsync(pasajero));
        }

        [Fact]
        public async Task Historial_ordenadoDelMasNuevoAlMasViejo()
        {
            var pasajero = _base.CrearPasajero();
            var ids = new List<int>();
            for (var i = 0; i < 3; i++)
            {
                var r = await Servicio().PublicarAsync(pasajero, "Origen " + i, _ahora.AddMinutes(i));
                Assert.True(r.Ok, r.Error);
                Assert.True((await Servicio().CancelarAsync(r.ViajeId!.Value, pasajero, Motivo, _ahora.AddMinutes(i))).Ok);
                ids.Add(r.ViajeId.Value);
            }

            var historial = await Servicio().HistorialAsync(pasajero);

            ids.Reverse();
            Assert.Equal(ids, historial.Select(v => v.Id));
        }
    }
}
