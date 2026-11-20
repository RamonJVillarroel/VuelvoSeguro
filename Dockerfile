# Dockerfile de VuelvoSeguro (ASP.NET Core MVC, .NET 9)
# Se construye desde la raíz del repositorio:
#   docker build -t vuelvoseguro .

# ---------- Etapa 1: compilar y publicar ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Primero solo el .csproj, así la restauración de paquetes queda en caché
# y no se repite cada vez que cambia el código.
COPY VuelvoSeguro/VuelvoSeguro/VuelvoSeguro.csproj VuelvoSeguro/VuelvoSeguro/
RUN dotnet restore VuelvoSeguro/VuelvoSeguro/VuelvoSeguro.csproj

# Después el resto del código del proyecto web.
COPY VuelvoSeguro/VuelvoSeguro/ VuelvoSeguro/VuelvoSeguro/
RUN dotnet publish VuelvoSeguro/VuelvoSeguro/VuelvoSeguro.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- Etapa 2: imagen final, solo con el runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Carpetas que tienen que sobrevivir a un reinicio del contenedor (se montan como volúmenes):
#  - App_Data/documentos: documentos que suben los conductores (RF04).
#  - /home/app/.aspnet/DataProtection-Keys: claves de las cookies de sesión;
#    si se pierden, todos los usuarios quedan deslogueados.
RUN mkdir -p /app/App_Data/documentos /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /app/App_Data /home/app/.aspnet

COPY --from=build --chown=app:app /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080

# El contenedor corre con un usuario sin privilegios (viene en la imagen oficial).
USER app
EXPOSE 8080

ENTRYPOINT ["dotnet", "VuelvoSeguro.dll"]
