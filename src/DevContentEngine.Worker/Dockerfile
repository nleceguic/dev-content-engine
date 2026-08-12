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
COPY src/DevContentEngine.Worker/DevContentEngine.Worker.csproj src/DevContentEngine.Worker/
RUN dotnet restore src/DevContentEngine.Worker/DevContentEngine.Worker.csproj

COPY src/DevContentEngine.Domain/ src/DevContentEngine.Domain/
COPY src/DevContentEngine.Application/ src/DevContentEngine.Application/
COPY src/DevContentEngine.Infrastructure/ src/DevContentEngine.Infrastructure/
COPY src/DevContentEngine.Worker/ src/DevContentEngine.Worker/

RUN dotnet publish src/DevContentEngine.Worker/DevContentEngine.Worker.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---- Runtime ------------------------------------------------------------------------------------
# aspnet:9.0-alpine, not the even smaller "chiseled" variant: tzdata below needs apk, and the
# chiseled image has no package manager to install it with.
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# tzdata: Program.cs resolves "Europe/Madrid" via TimeZoneInfo.FindSystemTimeZoneById on startup to
# schedule the daily job — the bare alpine base image ships no IANA time zone database at all, and
# without it the Worker crashes on every boot with TimeZoneNotFoundException.
RUN apk add --no-cache tzdata

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

# Program.cs writes rolling file logs to <BaseDirectory>/logs; create it and hand ownership to the
# non-root "app" user (built into the base image as $APP_UID) before dropping root.
RUN mkdir -p /app/logs && chown -R $APP_UID:$APP_UID /app

EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "DevContentEngine.Worker.dll"]
