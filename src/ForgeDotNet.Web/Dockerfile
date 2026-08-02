# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0.302-alpine3.23@sha256:d8ee39817ca03a3757288e83c37ed73cc969a286c603b827c7cbe33add1c2d1c AS build
WORKDIR /source

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/ForgeDotNet.Domain/ForgeDotNet.Domain.csproj src/ForgeDotNet.Domain/
COPY src/ForgeDotNet.Application/ForgeDotNet.Application.csproj src/ForgeDotNet.Application/
COPY src/ForgeDotNet.Infrastructure/ForgeDotNet.Infrastructure.csproj src/ForgeDotNet.Infrastructure/
COPY src/ForgeDotNet.CodeRunner/ForgeDotNet.CodeRunner.csproj src/ForgeDotNet.CodeRunner/
COPY src/ForgeDotNet.Web/ForgeDotNet.Web.csproj src/ForgeDotNet.Web/
RUN --mount=type=cache,id=forge-dotnet-nuget,target=/root/.nuget/packages \
    dotnet restore src/ForgeDotNet.Web/ForgeDotNet.Web.csproj --disable-parallel

COPY src/ src/
RUN --mount=type=cache,id=forge-dotnet-nuget,target=/root/.nuget/packages \
    dotnet publish src/ForgeDotNet.Web/ForgeDotNet.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.23@sha256:27b6b84beeede74fd16886177d360799c8e4299ceadfbd64eef57bafead7878a AS runtime
WORKDIR /app

RUN apk add --no-cache icu-libs \
    && mkdir -p /var/lib/forge-dotnet \
    && chown -R app:app /var/lib/forge-dotnet

COPY --from=build /app/publish ./
COPY content/schemas/ ./content/schemas/
COPY content/reference/ ./content/reference/
COPY content/diagnostic/ ./content/diagnostic/
COPY content/planning/ ./content/planning/
COPY src/ForgeDotNet.Web/docker-entrypoint.sh /usr/local/bin/forge-dotnet-entrypoint
RUN chmod 0555 /usr/local/bin/forge-dotnet-entrypoint

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["/usr/local/bin/forge-dotnet-entrypoint"]
