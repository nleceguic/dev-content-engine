# syntax=docker/dockerfile:1
#
# Build context is the repository root (see docker-compose.yml), because this project references
# Application, Infrastructure and Domain as sibling projects.

# ---- Build ------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore first with only the .csproj files present, so this layer stays cached across builds
# unless a project reference or package version actually changes.
COPY src/DevContentEngine.Domain/DevContentEngine.Domain.csproj src/DevContentEngine.Domain/
COPY src/DevContentEngine.Application/DevContentEngine.Application.csproj src/DevContentEngine.Application/
COPY src/DevContentEngine.Infrastructure/DevContentEngine.Infrastructure.csproj src/DevContentEngine.Infrastructure/
COPY src/DevContentEngine.Api/DevContentEngine.Api.csproj src/DevContentEngine.Api/
RUN dotnet restore src/DevContentEngine.Api/DevContentEngine.Api.csproj

COPY src/DevContentEngine.Domain/ src/DevContentEngine.Domain/
COPY src/DevContentEngine.Application/ src/DevContentEngine.Application/
COPY src/DevContentEngine.Infrastructure/ src/DevContentEngine.Infrastructure/
COPY src/DevContentEngine.Api/ src/DevContentEngine.Api/

RUN dotnet publish src/DevContentEngine.Api/DevContentEngine.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---- Runtime ------------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "DevContentEngine.Api.dll"]
