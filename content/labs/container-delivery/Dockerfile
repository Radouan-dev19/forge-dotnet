# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0.302-alpine3.23@sha256:d8ee39817ca03a3757288e83c37ed73cc969a286c603b827c7cbe33add1c2d1c AS build
WORKDIR /source
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY content/labs/api-mini-erp/src/ForgeApiLab/ForgeApiLab.csproj content/labs/api-mini-erp/src/ForgeApiLab/
RUN --mount=type=cache,id=forge-s11-s20-nuget,target=/root/.nuget/packages dotnet restore content/labs/api-mini-erp/src/ForgeApiLab/ForgeApiLab.csproj
COPY content/labs/api-mini-erp/src/ForgeApiLab/ content/labs/api-mini-erp/src/ForgeApiLab/
RUN --mount=type=cache,id=forge-s11-s20-nuget,target=/root/.nuget/packages dotnet publish content/labs/api-mini-erp/src/ForgeApiLab/ForgeApiLab.csproj --configuration Release --no-restore --output /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine3.23@sha256:27b6b84beeede74fd16886177d360799c8e4299ceadfbd64eef57bafead7878a
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080 DOTNET_EnableDiagnostics=0
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "ForgeApiLab.dll"]
