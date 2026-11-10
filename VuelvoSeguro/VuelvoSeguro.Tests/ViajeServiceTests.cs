using VuelvoSeguro.Models;
using VuelvoSeguro.Servicios;

namespace VuelvoSeguro.Tests
{
    // Pruebas de las reglas del viaje: RF05 (publicar), RF07 (aceptar) y RF08 (estados).
    public class ViajeServiceTests : IDisposable
    {
        private readonly BaseDeDatosDePrueba _base = new();
        private readonly DateTime _ahora = DateTime.Now;

        public void Dispose() => _base.Dispose();

        private ViajeService Servicio() => new(_base.CrearContexto());

        private async Task<int> PublicarViajeAsync(int pasajeroId)
        {
            var resultado = await Servicio().PublicarAsync(pasajeroId, "Bar La Esquina, Av. Corrientes 1800", _ahora);
            Assert.True(resultado.Ok, resultado.Error);
            return resultado.ViajeId!.Value;
        }

        [Fact]
        public async Task Publicar_creaViajePublicadoHaciaLaCasaDelPasajero()
        {
            var pasajero = _base.CrearPasajero();

            var id = await PublicarViajeAsync(pasajero);

            var viaje = await Servicio().BuscarAsync(id);
            Assert.Equal(EstadoViaje.Publicado, viaje!.EstadoViajeId);
            Assert.Equal("Av. Rivadavia 4520", viaje.DireccionDestino);
            Assert.Null(viaje.ConductorId);
        }

        [Fact]
        public async Task Publicar_sinSeguroVigente_seRechaza()
        {
            var pasajero = _base.CrearPasajero(seguroVigente: false);

            var resultado = await Servicio().PublicarAsync(pasajero, "Av. Corrientes 1800", _ahora);

            Assert.False(resultado.Ok);
            Assert.Contains("seguro", resultado.Error);
        }

        [Fact]
        public async Task Publicar_sinOrigen_seRechaza()
        {
            var pasajero = _base.CrearPasajero();

            var resultado = await Servicio().PublicarAsync(pasajero, "   ", _ahora);

            Assert.False(resultado.Ok);
        }

        [Fact]
        public async Task Publicar_conUnViajeActivo_seRechaza()
        {
            var pasajero = _base.CrearPasajero();
            await PublicarViajeAsync(pasajero);

            var resultado = await Servicio().PublicarAsync(pasajero, "Otro lugar 123", _ahora);

            Assert.False(resultado.Ok);
            Assert.Equal("Ya tenés un viaje activo.", resultado.Error);
        }

        [Fact]
        public async Task Disponibles_muestraSoloLosViajesSinConductor()
        {
            var viaje1 = await PublicarViajeAsync(_base.CrearPasajero());
            var viaje2 = await PublicarViajeAsync(_base.CrearPasajero());
            await Servicio().AceptarAsync(viaje1, _base.CrearConductor());

            var disponibles = await Servicio().DisponiblesAsync();

            Assert.Equal(new[] { viaje2 }, disponibles.Select(v => v.Id));
        }

        [Fact]
        public async Task Aceptar_asignaElConductorYPasaAAceptado()
        {
            var viaje = await PublicarViajeAsync(_base.CrearPasajero());
            var conductor = _base.CrearConductor();

            var resultado = await Servicio().AceptarAsync(viaje, conductor);

            Assert.True(resultado.Ok, resultado.Error);
            var guardado = await Servicio().BuscarAsync(viaje);
            Assert.Equal(conductor, guardado!.ConductorId);
            Assert.Equal(EstadoViaje.Aceptado, guardado.EstadoViajeId);
        }

        [Fact]
        public async Task Aceptar_unViajeYaAceptado_noCambiaElConductor()
        {
            var viaje = await PublicarViajeAsync(_base.CrearPasajero());
            var primero = _base.CrearConductor();
            var segundo = _base.CrearConductor();
            await Servicio().AceptarAsync(viaje, primero);

            var resultado = await Servicio().AceptarAsync(viaje, segundo);

            Assert.False(resultado.Ok);
            Assert.Equal(primero, (await Servicio().BuscarAsync(viaje))!.ConductorId);
        }

        [Theory]
        [InlineData(EstadoUsuario.Pendiente)]
        [InlineData(EstadoUsuario.Rechazado)]
        public async Task Aceptar_conductorNoAprobado_seRechaza(int estado)
        {
            var viaje = await PublicarViajeAsync(_base.CrearPasajero());

            var resultado = await Servicio().AceptarAsync(viaje, _base.CrearConductor(estado));

            Assert.False(resultado.Ok);
        }

        [Fact]
        public async Task Aceptar_conductorConViajeActivo_seRechaza()
        {
            var conductor = _base.CrearConductor();
            await Servicio().AceptarAsync(await PublicarViajeAsync(_base.CrearPasajero()), conductor);
            var otroViaje = await PublicarViajeAsync(_base.CrearPasajero());

            var resultado = await Servicio().AceptarAsync(otroViaje, conductor);

            Assert.False(resultado.Ok);
        }

        [Fact]
        public async Task CicloCompleto_publicadoAceptadoEnCursoFinalizado()
        {
            var pasajero = _base.CrearPasajero();
            var conductor = _base.CrearConductor();
            var viaje = await PublicarViajeAsync(pasajero);

            Assert.True((await Servicio().AceptarAsync(viaje, conductor)).Ok);
            Assert.True((await Servicio().IniciarAsync(viaje, conductor, _ahora)).Ok);
            Assert.True((await Servicio().FinalizarAsync(viaje, conductor, _ahora.AddMinutes(20))).Ok);

            var final = await Servicio().BuscarAsync(viaje);
            Assert.Equal(EstadoViaje.Finalizado, final!.EstadoViajeId);
            Assert.NotNull(final.FechaViajeInicio);
            Assert.NotNull(final.FechaViajeFin);
            Assert.Null(await Servicio().ViajeActivoDelPasajeroAsync(pasajero));
            Assert.Null(await Servicio().ViajeActivoDelConductorAsync(conductor));
        }

        [Fact]
        public async Task Finalizar_sinHaberIniciado_seRechaza()
        {
            var conductor = _base.CrearConductor();
            var viaje = await PublicarViajeAsync(_base.CrearPasajero());
            await Servicio().AceptarAsync(viaje, conductor);

            var resultado = await Servicio().FinalizarAsync(viaje, conductor, _ahora);

            Assert.False(resultado.Ok);
            Assert.Equal(EstadoViaje.Aceptado, (await Servicio().BuscarAsync(viaje))!.EstadoViajeId);
        }

        [Fact]
        public async Task Iniciar_porOtroConductor_seRechaza()
        {
            var viaje = await PublicarViajeAsync(_base.CrearPasajero());
            await Servicio().AceptarAsync(viaje, _base.CrearConductor());

            var resultado = await Servicio().IniciarAsync(viaje, _base.CrearConductor(), _ahora);

            Assert.False(resultado.Ok);
        }
    }
}
