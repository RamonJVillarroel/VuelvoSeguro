namespace VuelvoSeguro.Servicios
{
    // RNF03: encabezados de seguridad en todas las respuestas.
    public static class EncabezadosSeguridad
    {
        public static readonly IReadOnlyDictionary<string, string> Valores = new Dictionary<string, string>
        {
            // El navegador no adivina el tipo de los archivos (evita ejecutar un documento subido como script).
            ["X-Content-Type-Options"] = "nosniff",
            // Nadie puede mostrar la plataforma dentro de un iframe (clickjacking).
            ["X-Frame-Options"] = "DENY",
            ["Content-Security-Policy"] = "frame-ancestors 'none'; base-uri 'self'; form-action 'self'; object-src 'none'",
            // No se envía la URL completa (con ids de viajes) a otros sitios.
            ["Referrer-Policy"] = "strict-origin-when-cross-origin",
            ["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self)"
        };

        public static void Aplicar(IHeaderDictionary encabezados)
        {
            foreach (var (nombre, valor) in Valores)
            {
                encabezados[nombre] = valor;
            }
        }

        public static IApplicationBuilder UseEncabezadosSeguridad(this IApplicationBuilder app) =>
            app.Use(async (contexto, siguiente) =>
            {
                Aplicar(contexto.Response.Headers);
                await siguiente();
            });
    }
}
