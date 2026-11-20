# Deploy en Railway con Docker

VuelvoSeguro es ASP.NET Core MVC sobre .NET 9 y usa SQL Server. Railway detecta el `Dockerfile` en la raíz
del repositorio y arma el contenedor solo.

## Archivos

- `Dockerfile`: compila con el SDK de .NET 9 y publica en una imagen con solo el runtime de ASP.NET.
  Escucha en el puerto que Railway pasa en la variable `PORT` (8080 si no existe).
- `.dockerignore`: deja afuera bin/obj, .vs, .git, pruebas y documentos subidos.
- `docker-compose.yml`: solo para probar en tu PC lo mismo que corre en Railway (Railway no lo usa).
- `.env.example`: contraseñas de ejemplo para el compose local.

## Pasos en Railway

1. Hacer push de la rama con el `Dockerfile` a GitHub.
2. En Railway: **New Project → Deploy from GitHub repo** → elegir `VuelvoSeguro`. Railway ve el `Dockerfile` y lo usa.
3. **Base de datos.** Railway no tiene SQL Server administrado, así que se agrega como otro servicio:
   **+ New → Docker Image** → `mcr.microsoft.com/mssql/server:2022-latest`, con estas variables:
   - `ACCEPT_EULA` = `Y`
   - `MSSQL_PID` = `Express`
   - `MSSQL_SA_PASSWORD` = una clave fuerte (mayúscula, minúscula, número y símbolo, 8+ caracteres)

   y un volumen montado en `/var/opt/mssql` para que la base no se borre. SQL Server pide unos 2 GB de RAM.
   (Alternativa: una base Azure SQL y usar su cadena de conexión.)
4. En el servicio de la app, pestaña **Variables**:
   - `ConnectionStrings__DefaultConnection` =
     `Server=NOMBRE-DEL-SERVICIO-SQL.railway.internal,1433;Database=VuelvoSeguroDb;User Id=sa;Password=LA_CLAVE;TrustServerCertificate=True;MultipleActiveResultSets=true`
     (el nombre interno sale en Settings → Networking del servicio SQL Server)
   - `AdminInicial__Email` = mail del administrador
   - `AdminInicial__Password` = clave del administrador (8+ caracteres con un número)
5. En el servicio de la app: **+ Volume** montado en `/app/App_Data`. Ahí quedan los documentos de los
   conductores y las claves de sesión, que así sobreviven a cada redeploy.
6. **Settings → Networking → Generate Domain** para tener la URL pública (Railway ya pone el HTTPS).

Al arrancar, la app aplica las migraciones y crea los roles y el administrador.

## Probar en tu PC antes de subir

Con Docker Desktop, desde la carpeta del repositorio:

```bash
copy .env.example .env
docker compose up -d --build
```

Abrir http://localhost:8080. `docker compose logs -f web` muestra los logs y `docker compose down` apaga
(`down -v` además borra la base y los documentos).
