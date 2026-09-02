FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY src/DataBridge.Domain/DataBridge.Domain.csproj src/DataBridge.Domain/
COPY src/DataBridge.Application/DataBridge.Application.csproj src/DataBridge.Application/
COPY src/DataBridge.Infrastructure/DataBridge.Infrastructure.csproj src/DataBridge.Infrastructure/
COPY src/DataBridge.Cli/DataBridge.Cli.csproj src/DataBridge.Cli/

RUN dotnet restore src/DataBridge.Cli/DataBridge.Cli.csproj

COPY src ./src

RUN dotnet publish src/DataBridge.Cli/DataBridge.Cli.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
COPY samples ./samples

ENTRYPOINT ["dotnet", "DataBridge.Cli.dll"]
