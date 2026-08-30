# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only what restore needs first, so a source-only change does not
# invalidate the restore layer.
COPY backend/Directory.Build.props backend/Directory.Packages.props backend/global.json ./backend/
COPY backend/NaHora.slnx ./backend/
COPY backend/src/NaHora.Api/NaHora.Api.csproj                       ./backend/src/NaHora.Api/
COPY backend/src/NaHora.Domain/NaHora.Domain.csproj                 ./backend/src/NaHora.Domain/
COPY backend/src/NaHora.Features/NaHora.Features.csproj             ./backend/src/NaHora.Features/
COPY backend/src/NaHora.Infrastructure/NaHora.Infrastructure.csproj ./backend/src/NaHora.Infrastructure/
COPY backend/src/NaHora.Shared/NaHora.Shared.csproj                 ./backend/src/NaHora.Shared/

RUN dotnet restore backend/src/NaHora.Api/NaHora.Api.csproj

COPY backend/ ./backend/

RUN dotnet publish backend/src/NaHora.Api/NaHora.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

# ---------------------------------------------------------------------------
# Runtime
#
# Deliberately NOT alpine or chiseled: OpenCvSharp and ONNX Runtime load native
# libraries that need glibc and libgomp. Those images produce a native load
# failure that is slow and confusing to diagnose, in exchange for a smaller
# image we do not need.
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

RUN apt-get update \
    && apt-get install --yes --no-install-recommends \
        libgomp1 \
        curl \
    && rm -rf /var/lib/apt/lists/*

# Run unprivileged. A container process that does not need root must not have it.
RUN groupadd --system --gid 64198 nahora \
    && useradd --system --uid 64198 --gid nahora --no-create-home nahora

WORKDIR /app
COPY --from=build --chown=nahora:nahora /app/publish .

# ONNX models are mounted at runtime, never baked in: they are large and some
# carry licence restrictions that must not travel inside a distributed image.
RUN mkdir -p /app/models /app/certs && chown nahora:nahora /app/models /app/certs

USER nahora

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["dotnet", "NaHora.Api.dll"]
