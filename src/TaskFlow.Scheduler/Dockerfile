# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG NUGET_TOKEN=""
WORKDIR /source

# Copy solution + package management files for restore-layer caching
COPY src/TaskFlow.slnx src/Directory.Build.props src/Directory.Packages.props src/global.json src/nuget.config ./src/

# Copy csproj files for restore
COPY src/TaskFlow.Scheduler/TaskFlow.Scheduler.csproj src/TaskFlow.Scheduler/
COPY src/TaskFlow.Bootstrapper/TaskFlow.Bootstrapper.csproj src/TaskFlow.Bootstrapper/
COPY src/Application/TaskFlow.Application.Contracts/TaskFlow.Application.Contracts.csproj src/Application/TaskFlow.Application.Contracts/
COPY src/Application/TaskFlow.Application.Models/TaskFlow.Application.Models.csproj src/Application/TaskFlow.Application.Models/
COPY src/Application/TaskFlow.Application.Mappers/TaskFlow.Application.Mappers.csproj src/Application/TaskFlow.Application.Mappers/
COPY src/Application/TaskFlow.Application.Services/TaskFlow.Application.Services.csproj src/Application/TaskFlow.Application.Services/
COPY src/Application/TaskFlow.Application.MessageHandlers/TaskFlow.Application.MessageHandlers.csproj src/Application/TaskFlow.Application.MessageHandlers/
COPY src/Domain/TaskFlow.Domain.Model/TaskFlow.Domain.Model.csproj src/Domain/TaskFlow.Domain.Model/
COPY src/Domain/TaskFlow.Domain.Shared/TaskFlow.Domain.Shared.csproj src/Domain/TaskFlow.Domain.Shared/
COPY src/Infrastructure/TaskFlow.Infrastructure.Data/TaskFlow.Infrastructure.Data.csproj src/Infrastructure/TaskFlow.Infrastructure.Data/
COPY src/Infrastructure/TaskFlow.Infrastructure.Repositories/TaskFlow.Infrastructure.Repositories.csproj src/Infrastructure/TaskFlow.Infrastructure.Repositories/
COPY src/Aspire/ServiceDefaults/ServiceDefaults.csproj src/Aspire/ServiceDefaults/

# Configure private NuGet feed auth (if token provided)
RUN if [ -n "$NUGET_TOKEN" ]; then \
      dotnet nuget update source efreeman518-github \
        --configfile src/nuget.config \
        --username docker --password "$NUGET_TOKEN" --store-password-in-clear-text; \
    fi

RUN dotnet restore src/TaskFlow.Scheduler/TaskFlow.Scheduler.csproj

# Copy source and build
COPY src/ src/
RUN dotnet publish src/TaskFlow.Scheduler/TaskFlow.Scheduler.csproj -c Release -o /app --no-restore

# Runtime — no port exposed (background worker)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app .
ENTRYPOINT ["dotnet", "TaskFlow.Scheduler.dll"]
