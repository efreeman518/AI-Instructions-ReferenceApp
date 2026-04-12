# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG NUGET_TOKEN=""
WORKDIR /source

# Copy solution + package management files for restore-layer caching
COPY src/TaskFlow.slnx src/Directory.Build.props src/Directory.Packages.props src/global.json src/nuget.config ./src/

# Copy csproj files for restore
COPY src/Host/TaskFlow.Gateway/TaskFlow.Gateway.csproj src/Host/TaskFlow.Gateway/
COPY src/Host/Aspire/ServiceDefaults/ServiceDefaults.csproj src/Host/Aspire/ServiceDefaults/

# Configure private NuGet feed auth (if token provided)
RUN if [ -n "$NUGET_TOKEN" ]; then \
      dotnet nuget update source efreeman518-github \
        --configfile src/nuget.config \
        --username docker --password "$NUGET_TOKEN" --store-password-in-clear-text; \
    fi

RUN dotnet restore src/Host/TaskFlow.Gateway/TaskFlow.Gateway.csproj

# Copy source and build
COPY src/ src/
RUN dotnet publish src/Host/TaskFlow.Gateway/TaskFlow.Gateway.csproj -c Release -o /app --no-restore

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app .
ENTRYPOINT ["dotnet", "TaskFlow.Gateway.dll"]
