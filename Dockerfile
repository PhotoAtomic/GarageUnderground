# Build stage using the .NET 10 SDK
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy project files for restore
COPY src/GarageUnderground/GarageUnderground/GarageUnderground.csproj GarageUnderground/GarageUnderground/
COPY src/GarageUnderground/GarageUnderground.Client/GarageUnderground.Client.csproj GarageUnderground/GarageUnderground.Client/
COPY src/GarageUnderground.ServiceDefaults/GarageUnderground.ServiceDefaults.csproj GarageUnderground.ServiceDefaults/

# Restore dependencies
RUN dotnet restore GarageUnderground/GarageUnderground/GarageUnderground.csproj

# Copy source code
COPY src/GarageUnderground/ GarageUnderground/
COPY src/GarageUnderground.ServiceDefaults/ GarageUnderground.ServiceDefaults/

# Publish the application
RUN dotnet publish GarageUnderground/GarageUnderground/GarageUnderground.csproj \
    -c Release \
    -o /app/publish

# Runtime stage - ASP.NET runtime image (required for Blazor WebAssembly hosting)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# Run as root to ensure write access to mounted volumes
USER 0
WORKDIR /app

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Expose port
EXPOSE 8080

# Create data directory for database
RUN mkdir -p /data
VOLUME ["/data"]

# Copy published application
COPY --from=build /app/publish ./

# Set entrypoint
ENTRYPOINT ["dotnet", "GarageUnderground.dll"]
