# TimeVerse deployment checklist

## SQL Server

1. Set `ConnectionStrings__DefaultConnection` as an environment variable or secret.
2. Replace the development SQLite provider in `Program.cs` with `UseSqlServer(...)` after installing `Microsoft.EntityFrameworkCore.SqlServer`.
3. Create and apply EF Core migrations: `dotnet ef migrations add InitialCommerceSchema` then `dotnet ef database update`.
4. Set a strong `SeedAdmin__Password` value through environment variables; never use the development credential in production.

## IIS or Azure App Service

1. Publish with `dotnet publish -c Release -o publish`.
2. Set `ASPNETCORE_ENVIRONMENT=Production` and configure the connection string and seed password as secrets.
3. Grant the application identity write access to `wwwroot/uploads` and the Data Protection keys directory, or configure a shared key store.
4. Enable HTTPS, configure a domain, and verify `/openapi/v1.json` only in the intended environment.

## Docker

Build from the published output using the .NET 10 ASP.NET runtime image. Inject connection strings and secrets at runtime; do not put credentials in the image.
