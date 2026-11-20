using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using VuelvoSeguro.Servicios;
using VuelvoSeguro.ViewModels;

namespace VuelvoSeguro.Tests
{
    // Pruebas del sprint 6: términos y condiciones (RNF08), encabezados de seguridad (RNF03)
    // y armado del motivo de cancelación (RF09).
    public class ValidacionesYSeguridadTests
    {
        private static RegistroConductorViewModel RegistroValido(bool aceptaTerminos) => new()
        {
            Nombre = "Martín",
            Apellido = "Pérez",
            Dni = "30123456",
            FechaNacimiento = new DateTime(1990, 5, 10),
            Email = "martin@mail.com",
            Telefono = "1155554444",
            Password = "clave1234",
            ConfirmarPassword = "clave1234",
            AceptaTerminos = aceptaTerminos
        };

        private static List<ValidationResult> Validar(object modelo)
        {
            var errores = new List<ValidationResult>();
            Validator.TryValidateObject(modelo, new ValidationContext(modelo), errores, validateAllProperties: true);
            return errores;
        }

        [Fact]
        public void Registro_sinAceptarTerminos_esInvalido()
        {
            var errores = Validar(RegistroValido(aceptaTerminos: false));

            var error = Assert.Single(errores);
            Assert.Contains(nameof(RegistroViewModel.AceptaTerminos), error.MemberNames);
        }

        [Fact]
        public void Registro_aceptandoTerminos_esValido()
        {
            Assert.Empty(Validar(RegistroValido(aceptaTerminos: true)));
        }

        [Fact]
        public void EncabezadosSeguridad_seAgreganALaRespuesta()
        {
            var contexto = new DefaultHttpContext();

            EncabezadosSeguridad.Aplicar(contexto.Response.Headers);

            var h = contexto.Response.Headers;
            Assert.Equal("nosniff", h["X-Content-Type-Options"]);
            Assert.Equal("DENY", h["X-Frame-Options"]);
            Assert.Contains("frame-ancestors 'none'", h["Content-Security-Policy"].ToString());
            Assert.Equal("strict-origin-when-cross-origin", h["Referrer-Policy"]);
        }

        [Fact]
        public void Motivo_elegidoDeLaLista_seUsaTalCual()
        {
            Assert.Equal("Me quedo un rato más", CancelacionHelper.Motivo("Me quedo un rato más", "ignorado"));
        }

        [Fact]
        public void Motivo_otro_usaElTextoEscrito()
        {
            Assert.Equal("Se me pinchó una rueda", CancelacionHelper.Motivo(CancelacionHelper.Otro, "  Se me pinchó una rueda "));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", "algo")]
        [InlineData(CancelacionHelper.Otro, null)]
        [InlineData(CancelacionHelper.Otro, "   ")]
        public void Motivo_vacio_quedaSinMotivo(string? elegido, string? otro)
        {
            Assert.True(string.IsNullOrEmpty(CancelacionHelper.Motivo(elegido, otro)));
        }
    }
}
