# ==========================================================
# TimeVerse Luxury Horology E-Commerce - Multi-Stage Dockerfile
# Target: ASP.NET Core 10.0 Production Image
# ==========================================================

# 1. Base Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_URLS="http://+:8080"
ENV ASPNETCORE_ENVIRONMENT="Production"

# 2. Build SDK Image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies (optimizing docker layer cache)
COPY ["TimeVerse.csproj", "./"]
RUN dotnet restore "TimeVerse.csproj"

# Copy full application source code and compile
COPY . .
RUN dotnet build "TimeVerse.csproj" -c Release -o /app/build

# 3. Publish Release Artifacts
FROM build AS publish
RUN dotnet publish "TimeVerse.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Final Minimal Production Image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directory for persistent uploads
RUN mkdir -p /app/wwwroot/images/uploads

ENTRYPOINT ["dotnet", "TimeVerse.dll"]
