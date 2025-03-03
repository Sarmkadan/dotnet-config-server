# Migration Guide: v1.x to v2.0.0

This guide covers the breaking changes and migration steps required to upgrade from Dotnet Config Server v1.x to v2.0.0.

## Overview

Version 2.0.0 introduces production-ready Docker support, enhanced containerization, and improved deployment workflows. While the API surface remains stable, deployment and infrastructure configurations have significant changes.

## Key Changes

### 1. Docker Support (NEW)

#### Multi-Stage Dockerfile
- v1.x: Basic single-stage Docker support
- v2.0: Production-optimized multi-stage builds with builder, publish, and runtime stages
- **Impact**: Container images are now significantly smaller and more efficient
- **Action**: Rebuild containers using the updated Dockerfile

#### Health Check Configuration
- v1.x: Basic HEALTHCHECK endpoint
- v2.0: Standardized HEALTHCHECK with configurable intervals, timeouts, and retries
```dockerfile
# v2.0 - New HEALTHCHECK directive
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1
```
- **Impact**: Docker now automatically monitors container health
- **Action**: No changes required; automatically applied with new Dockerfile

### 2. Docker Compose Enhancements

#### Service Health Checks
- Both `mssql` and `app` services now include health check configurations
- `app` service waits for `mssql` service to be healthy before starting
```yaml
depends_on:
  mssql:
    condition: service_healthy
```
- **Impact**: Prevents failed startup sequences
- **Action**: Update your docker-compose.yml to v2.0 version

#### Environment Variable Standardization
- All environment variables now follow consistent naming conventions
- Connection strings use `ConnectionStrings__` prefix (double underscore)
- Application settings use `ApplicationSettings__` prefix
- **Impact**: Improved security and configuration management
- **Action**: Update your .env files and Docker environment configurations

### 3. Deployment Changes

#### Recommended Deployment Method
- v1.x: Supported multiple deployment methods (standalone .exe, service, container)
- v2.0: Containerization is the recommended primary deployment method
- **Impact**: Simplified infrastructure and improved consistency
- **Action**: Migrate to container-based deployments

#### Port Configuration
- Default port remains `8080` (internal)
- In docker-compose.yml, exposed as `80:8080` and `443:8443`
- **Impact**: Ensure firewall rules and load balancer configurations are updated
- **Action**: Review port mappings in your infrastructure

### 4. File Structure and Volumes

#### New Volume Requirements
- `/app/logs` - Application logs directory (recommended to mount externally)
- `/app/config` - Configuration files directory (optional, for advanced scenarios)
```yaml
volumes:
  - ./logs:/app/logs
  - ./config:/app/config
```
- **Impact**: Logs and configs persist outside container lifecycle
- **Action**: Ensure host directories exist with proper permissions

### 5. Build Optimization

#### Layer Caching
- v2.0 Dockerfile optimized for Docker layer caching
- Dependencies restored separately from source code
- **Impact**: Faster builds in CI/CD when only application code changes
- **Action**: Use new Dockerfile in CI/CD pipelines

## Migration Steps

### Step 1: Backup Current Configuration
```bash
# Backup database
docker exec dotnet-config-db sqlcmd -S localhost -U sa -P "YourPassword" \
  -Q "BACKUP DATABASE DotnetConfigServerDb TO DISK='/var/opt/mssql/data/backup.bak'"

# Backup config files
cp -r /path/to/config ./config.backup
```

### Step 2: Update Dockerfile and Docker Compose
```bash
# Replace with v2.0 Dockerfile and docker-compose.yml
git pull origin main
```

### Step 3: Rebuild and Deploy Containers
```bash
# Stop existing containers
docker-compose down

# Remove old images (optional)
docker rmi dotnet-config-server:latest mcr.microsoft.com/mssql/server:latest

# Build and start new containers
docker-compose up -d --build
```

### Step 4: Verify Health Checks
```bash
# Check container status
docker-compose ps

# Verify health status
docker inspect --format='{{.State.Health.Status}}' dotnet-config-server
docker inspect --format='{{.State.Health.Status}}' dotnet-config-db

# Check logs
docker-compose logs -f app
docker-compose logs -f mssql
```

### Step 5: Test API Connectivity
```bash
# Health check endpoint
curl -v http://localhost:8080/health

# API endpoint example
curl -v http://localhost:8080/api/v1/configurations
```

### Step 6: Update Monitoring and Alerting
- Update container monitoring to watch health check status
- Configure alerts for health check failures
- Update log aggregation paths to point to volume mount locations

## Breaking Changes

### API Changes
None - API v1 remains fully compatible

### Configuration Changes
- Environment variable names standardized (double underscores)
- Database connection string format unchanged

### Infrastructure Changes
- Docker volumes must be explicitly defined
- Health check intervals and timeouts are now configurable
- Container restart policy changed to `unless-stopped`

## Rollback Procedure

If you need to rollback to v1.x:

```bash
# Stop and remove v2.0 containers
docker-compose down

# Checkout previous version
git checkout tags/v1.0.0

# Restore database backup
docker run -v /path/to/backup:/backup --rm -it mcr.microsoft.com/mssql/server \
  /opt/mssql-tools/bin/sqlcmd -S <server> -U sa -P <password> \
  -Q "RESTORE DATABASE DotnetConfigServerDb FROM DISK='/backup/backup.bak'"

# Start v1.x containers
docker-compose up -d
```

## Performance Improvements in v2.0

1. **Faster Container Startup**: Multi-stage build removes SDK from runtime image
2. **Reduced Image Size**: Runtime image ~40% smaller than v1.x
3. **Better Layer Caching**: Dependencies cached separately from application code
4. **Health Check Optimization**: Native Docker health checks reduce external monitoring overhead

## Compatibility Matrix

| Component | v1.x | v2.0.0 |
|-----------|------|--------|
| .NET Target | 10.0 | 10.0 |
| SQL Server | 2019+ | 2019+ |
| Docker | 20.10+ | 20.10+ |
| Docker Compose | 1.29+ | 1.29+ |
| Kubernetes | 1.20+ | 1.20+ |

## Support and Issues

For migration issues or questions:
- Review the deployment.md guide for detailed deployment instructions
- Check logs: `docker-compose logs app mssql`
- Open an issue on [GitHub Issues](https://github.com/sarmkadan/dotnet-config-server/issues)
- Include version numbers, error messages, and environment details

## What's Next

After successfully migrating to v2.0.0:
- Explore the enhanced Kubernetes deployment guide
- Consider implementing container orchestration (ECS, AKS, GKE)
- Review the security documentation for production hardening
- Set up automated backups for your SQL Server container

---

Built by [Vladyslav Zaiets](https://sarmkadan.com)
