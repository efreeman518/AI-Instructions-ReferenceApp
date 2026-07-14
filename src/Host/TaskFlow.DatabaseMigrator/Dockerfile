# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG NUGET_TOKEN=""
WORKDIR /source

COPY TaskFlow.slnx Directory.Build.props Directory.Packages.props global.json nuget.config ./

COPY src/Host/TaskFlow.DatabaseMigrator/TaskFlow.DatabaseMigrator.csproj src/Host/TaskFlow.DatabaseMigrator/
COPY src/Host/Aspire/ServiceDefaults/ServiceDefaults.csproj src/Host/Aspire/ServiceDefaults/
COPY src/Infrastructure/TaskFlow.Infrastructure.Data/TaskFlow.Infrastructure.Data.csproj src/Infrastructure/TaskFlow.Infrastructure.Data/
COPY src/Domain/TaskFlow.Domain.Model/TaskFlow.Domain.Model.csproj src/Domain/TaskFlow.Domain.Model/
COPY src/Domain/TaskFlow.Domain.Shared/TaskFlow.Domain.Shared.csproj src/Domain/TaskFlow.Domain.Shared/

RUN if [ -n "$NUGET_TOKEN" ]; then \
    dotnet nuget update source efreeman518-github \
        --configfile nuget.config \
      --username github-actions \
      --password "$NUGET_TOKEN" \
      --store-password-in-clear-text; \
    fi

RUN dotnet restore src/Host/TaskFlow.DatabaseMigrator/TaskFlow.DatabaseMigrator.csproj

COPY src/ ./src/

RUN dotnet publish src/Host/TaskFlow.DatabaseMigrator/TaskFlow.DatabaseMigrator.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "TaskFlow.DatabaseMigrator.dll"]
