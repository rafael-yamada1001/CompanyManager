# ── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia os arquivos de projeto e restaura dependências (cache otimizado)
COPY src/CompanyManager.Domain/CompanyManager.Domain.csproj           src/CompanyManager.Domain/
COPY src/CompanyManager.Application/CompanyManager.Application.csproj src/CompanyManager.Application/
COPY src/CompanyManager.Infrastructure/CompanyManager.Infrastructure.csproj src/CompanyManager.Infrastructure/
COPY src/CompanyManager.API/CompanyManager.API.csproj                 src/CompanyManager.API/
RUN dotnet restore src/CompanyManager.API/CompanyManager.API.csproj

# Copia o restante e publica em modo Release
COPY . .
RUN dotnet publish src/CompanyManager.API/CompanyManager.API.csproj \
    -c Release -o /app/publish --no-restore

# ── Stage 2: Runtime ───────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Diretório persistente para o banco SQLite (mapear como volume)
RUN mkdir -p /var/data && chmod 777 /var/data

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "CompanyManager.API.dll"]
