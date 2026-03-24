# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder

WORKDIR /src

# Copy project files
COPY ["dotnet-config-server.csproj", "."]

# Restore dependencies
RUN dotnet restore "dotnet-config-server.csproj"

# Copy source code
COPY . .

# Build application
RUN dotnet build "dotnet-config-server.csproj" -c Release -o /app/build

# Publish stage
FROM builder AS publish

RUN dotnet publish "dotnet-config-server.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Expose port
EXPOSE 8080

# Set environment
ENV ASPNETCORE_URLS=http://+:8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Start application
ENTRYPOINT ["dotnet", "dotnet-config-server.dll"]
