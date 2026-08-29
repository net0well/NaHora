# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Build
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy only what restore needs first, so a source-only change does not
# invalidate the restore layer.
COPY backend/Directory.Build.props backend/Directory.Packages.props backend/global.json ./backend/
COPY backend/FaceId.slnx ./backend/
COPY backend/src/FaceId.Api/FaceId.Api.csproj                       ./backend/src/FaceId.Api/
COPY backend/src/FaceId.Domain/FaceId.Domain.csproj                 ./backend/src/FaceId.Domain/
COPY backend/src/FaceId.Features/FaceId.Features.csproj             ./backend/src/FaceId.Features/
COPY backend/src/FaceId.Infrastructure/FaceId.Infrastructure.csproj ./backend/src/FaceId.Infrastructure/
COPY backend/src/FaceId.Shared/FaceId.Shared.csproj                 ./backend/src/FaceId.Shared/

RUN dotnet restore backend/src/FaceId.Api/FaceId.Api.csproj

COPY backend/ ./backend/

RUN dotnet publish backend/src/FaceId.Api/FaceId.Api.csproj \
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
RUN groupadd --system --gid 64198 faceid \
    && useradd --system --uid 64198 --gid faceid --no-create-home faceid

WORKDIR /app
COPY --from=build --chown=faceid:faceid /app/publish .

# ONNX models are mounted at runtime, never baked in: they are large and some
# carry licence restrictions that must not travel inside a distributed image.
RUN mkdir -p /app/models /app/certs && chown faceid:faceid /app/models /app/certs

USER faceid

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["dotnet", "FaceId.Api.dll"]
