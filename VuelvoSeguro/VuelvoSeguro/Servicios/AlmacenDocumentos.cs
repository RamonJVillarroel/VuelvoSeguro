using Microsoft.AspNetCore.StaticFiles;

namespace VuelvoSeguro.Servicios
{
    // Guarda los documentos de los conductores en App_Data/documentos, fuera de wwwroot:
    // solo el administrador los puede ver, a través de AdminController.
    public class AlmacenDocumentos
    {
        private readonly string _carpeta;

        public AlmacenDocumentos(IWebHostEnvironment entorno)
        {
            _carpeta = Path.Combine(entorno.ContentRootPath, "App_Data", "documentos");
        }

        // Devuelve la ruta relativa que se guarda en la base (por ejemplo "12/licencia.jpg").
        public async Task<string> GuardarAsync(int usuarioId, string tipo, IFormFile archivo)
        {
            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
            var carpetaUsuario = Path.Combine(_carpeta, usuarioId.ToString());
            Directory.CreateDirectory(carpetaUsuario);

            // Si había un archivo anterior del mismo tipo (con otra extensión), se borra.
            foreach (var anterior in Directory.GetFiles(carpetaUsuario, tipo + ".*"))
            {
                File.Delete(anterior);
            }

            var nombre = tipo + extension;
            await using var destino = File.Create(Path.Combine(carpetaUsuario, nombre));
            await archivo.CopyToAsync(destino);
            return $"{usuarioId}/{nombre}";
        }

        public (string RutaCompleta, string TipoContenido)? Buscar(string? rutaRelativa)
        {
            if (string.IsNullOrEmpty(rutaRelativa))
            {
                return null;
            }
            var completa = Path.GetFullPath(Path.Combine(_carpeta, rutaRelativa));
            if (!completa.StartsWith(Path.GetFullPath(_carpeta)) || !File.Exists(completa))
            {
                return null;
            }
            if (!new FileExtensionContentTypeProvider().TryGetContentType(completa, out var tipo))
            {
                tipo = "application/octet-stream";
            }
            return (completa, tipo);
        }

        public static bool EsImagen(string? rutaRelativa) =>
            rutaRelativa != null && !rutaRelativa.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }
}
