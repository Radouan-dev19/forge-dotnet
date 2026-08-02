# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:d8ee39817ca03a3757288e83c37ed73cc969a286c603b827c7cbe33add1c2d1c AS build
WORKDIR /src
COPY NuGet.Config ./NuGet.Config
COPY RunnerHost/ForgeDotNet.RunnerHost.csproj RunnerHost/ForgeDotNet.RunnerHost.csproj
RUN dotnet restore RunnerHost/ForgeDotNet.RunnerHost.csproj --configfile NuGet.Config --disable-parallel
COPY RunnerSuiteContracts.cs RunnerSuiteContracts.cs
COPY RunnerHost/ RunnerHost/
RUN dotnet publish RunnerHost/ForgeDotNet.RunnerHost.csproj --configuration Release --no-restore --output /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine@sha256:d8ee39817ca03a3757288e83c37ed73cc969a286c603b827c7cbe33add1c2d1c
LABEL org.opencontainers.image.title="Forge.NET isolated code runner" \
      org.opencontainers.image.description="Ephemeral, non-root C# compilation and test worker" \
      org.opencontainers.image.source="https://local.invalid/forge-dotnet" \
      org.opencontainers.image.version="04C"

RUN apk add --no-cache libseccomp
COPY --from=build --chown=1654:1654 /out/ /opt/forge-runner/
COPY --chown=1654:1654 NuGet.Config /opt/forge-runner/NuGet.Config

ENV DOTNET_CLI_HOME=/workspace/.dotnet \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    HOME=/workspace/home \
    NUGET_PACKAGES=/workspace/.nuget/packages

USER 1654:1654
WORKDIR /workspace
ENTRYPOINT ["dotnet", "/opt/forge-runner/ForgeDotNet.RunnerHost.dll"]
