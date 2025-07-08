# Use the official .NET 8 runtime as the base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 6100

# Use the .NET 8 SDK for building the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Copy the project file and restore dependencies
COPY ["src/Api/Api.csproj", "src/Api/"]
RUN dotnet restore "src/Api/Api.csproj"

# Copy the entire source code
COPY . .
WORKDIR /src/Api

# Build the application
RUN dotnet build "Api.csproj" --no-restore -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Api.csproj" --no-restore -c Release -o /app/publish /p:UseAppHost=false

# Final stage - create the runtime image
FROM base AS final
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create a non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy the published application
COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:6100
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:6100/health/liveness || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Api.dll"]
