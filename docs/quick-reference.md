# Quick Reference Guide

Quick examples for common Dotnet Config Server operations.

## Installation

```bash
# Clone repository
git clone https://github.com/sarmkadan/dotnet-config-server.git
cd dotnet-config-server

# Build and run
./build.sh build && ./build.sh run
# OR on Windows
build.cmd build && build.cmd run
# OR with Docker
docker-compose up
```

API available at: `https://localhost:5001`
Swagger UI: `https://localhost:5001/swagger`

## Core API Endpoints

### Applications

```bash
# Create application
curl -X POST https://localhost:5001/api/v1/applications \
  -H "Content-Type: application/json" \
  -d '{"name":"OrderService","description":"Order processing"}'

# List applications
curl https://localhost:5001/api/v1/applications
```

### Configurations

```bash
# Create configuration
curl -X POST https://localhost:5001/api/v1/configurations \
  -H "Content-Type: application/json" \
  -d '{
    "applicationId":"APP_ID",
    "environment":"Production",
    "description":"Prod config"
  }'

# Get configuration
curl https://localhost:5001/api/v1/configurations/CONFIG_ID

# Update configuration
curl -X PUT https://localhost:5001/api/v1/configurations/CONFIG_ID \
  -H "Content-Type: application/json" \
  -d '{"description":"Updated description"}'

# Delete configuration
curl -X DELETE https://localhost:5001/api/v1/configurations/CONFIG_ID
```

### Configuration Keys

```bash
# Add key
curl -X POST https://localhost:5001/api/v1/configurations/CONFIG_ID/keys \
  -H "Content-Type: application/json" \
  -d '{
    "key":"Database:Host",
    "value":"localhost",
    "isEncrypted":false,
    "description":"Database hostname"
  }'

# Get all keys
curl https://localhost:5001/api/v1/configurations/CONFIG_ID/keys

# Update key
curl -X PUT https://localhost:5001/api/v1/configurations/CONFIG_ID/keys/KEY_ID \
  -H "Content-Type: application/json" \
  -d '{"value":"newvalue"}'

# Delete key
curl -X DELETE https://localhost:5001/api/v1/configurations/CONFIG_ID/keys/KEY_ID
```

### Versions

```bash
# Create version
curl -X POST https://localhost:5001/api/v1/configurations/CONFIG_ID/versions \
  -H "Content-Type: application/json" \
  -d '{
    "description":"Release v1.2.0",
    "changeNotes":"Added new features"
  }'

# List versions
curl https://localhost:5001/api/v1/configurations/CONFIG_ID/versions

# Get active version
curl https://localhost:5001/api/v1/configurations/CONFIG_ID/versions/active

# Publish version
curl -X POST https://localhost:5001/api/v1/configurations/CONFIG_ID/versions/VERSION_ID/publish \
  -H "Content-Type: application/json" \
  -d '{"notes":"Released to production"}'

# Compare versions
curl https://localhost:5001/api/v1/configurations/CONFIG_ID/versions/FROM_VERSION/diff/TO_VERSION

# Rollback version
curl -X POST https://localhost:5001/api/v1/configurations/CONFIG_ID/versions/VERSION_ID/rollback \
  -H "Content-Type: application/json" \
  -d '{"reason":"Bad configuration"}'
```

### Webhooks

```bash
# Subscribe to changes
curl -X POST https://localhost:5001/api/v1/configurations/CONFIG_ID/webhooks \
  -H "Content-Type: application/json" \
  -d '{
    "name":"My Webhook",
    "url":"https://example.com/webhook",
    "verifySignature":true
  }'

# List webhook subscriptions
curl https://localhost:5001/api/v1/configurations/CONFIG_ID/webhooks

# Delete subscription
curl -X DELETE https://localhost:5001/api/v1/configurations/CONFIG_ID/webhooks/WEBHOOK_ID

# Get delivery history
curl https://localhost:5001/api/v1/webhooks/WEBHOOK_ID/deliveries
```

### Audit Logs

```bash
# Get audit logs
curl https://localhost:5001/api/v1/configurations/CONFIG_ID/audit-logs

# Get logs with filters
curl "https://localhost:5001/api/v1/configurations/CONFIG_ID/audit-logs?action=ConfigurationUpdated&pageSize=50"
```

## CLI Commands

### Using the Build Scripts

```bash
# Linux/Mac
./build.sh install   # Install dependencies
./build.sh build     # Build project
./build.sh test      # Run tests
./build.sh run       # Run application
./build.sh clean     # Clean build artifacts
./build.sh docker    # Build Docker image
./build.sh format    # Format code

# Windows
build.cmd install
build.cmd build
build.cmd test
build.cmd run
build.cmd clean
build.cmd docker
build.cmd format
```

### Using Make

```bash
make help            # Show all available commands
make install         # Install dependencies
make build           # Build project
make test            # Run tests
make run             # Run locally
make clean           # Clean artifacts
make docker-build    # Build Docker image
make docker-up       # Start containers
make docker-down     # Stop containers
make db-migrate      # Apply migrations
make db-reset        # Reset database
```

### Using dotnet CLI Directly

```bash
dotnet build                    # Build
dotnet run                      # Run
dotnet test                     # Test
dotnet publish -c Release       # Publish
dotnet ef database update       # Apply migrations
dotnet ef database drop         # Drop database
dotnet format                   # Format code
```

## Common Tasks

### Set Up New Environment

```bash
# 1. Create application
APP_ID=$(curl -X POST https://localhost:5001/api/v1/applications \
  -H "Content-Type: application/json" \
  -d '{"name":"MyService"}' \
  | jq -r '.id')

# 2. Create development config
DEV_CONFIG=$(curl -X POST https://localhost:5001/api/v1/configurations \
  -H "Content-Type: application/json" \
  -d "{\"applicationId\":\"$APP_ID\",\"environment\":\"Development\"}" \
  | jq -r '.id')

# 3. Create production config
PROD_CONFIG=$(curl -X POST https://localhost:5001/api/v1/configurations \
  -H "Content-Type: application/json" \
  -d "{\"applicationId\":\"$APP_ID\",\"environment\":\"Production\"}" \
  | jq -r '.id')

echo "Created configs: Dev=$DEV_CONFIG, Prod=$PROD_CONFIG"
```

### Promote Configuration

```bash
# Get keys from dev config
KEYS=$(curl https://localhost:5001/api/v1/configurations/DEV_CONFIG_ID/keys)

# Add each key to prod config
echo "$KEYS" | jq '.[] | {key,value,isEncrypted}' | while read -r key; do
  curl -X POST https://localhost:5001/api/v1/configurations/PROD_CONFIG_ID/keys \
    -H "Content-Type: application/json" \
    -d "$key"
done
```

### Create and Publish Version

```bash
CONFIG_ID="your-config-id"

# 1. Create version
VERSION_ID=$(curl -X POST https://localhost:5001/api/v1/configurations/$CONFIG_ID/versions \
  -H "Content-Type: application/json" \
  -d '{"description":"Release v1.0"}' \
  | jq -r '.id')

# 2. Publish version
curl -X POST https://localhost:5001/api/v1/configurations/$CONFIG_ID/versions/$VERSION_ID/publish

echo "Published version: $VERSION_ID"
```

### Batch Import Configuration

```bash
# Using the BatchConfigurationImporter example
dotnet run examples/BatchConfigurationImporter.cs \
  --config-id "YOUR_CONFIG_ID" \
  --file "configurations.json"
```

### Export Configuration Backup

```bash
CONFIG_ID="your-config-id"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)

curl https://localhost:5001/api/v1/configurations/$CONFIG_ID \
  -H "Accept: application/json" \
  | jq . > config_backup_$TIMESTAMP.json

echo "Configuration exported to config_backup_$TIMESTAMP.json"
```

## Docker Commands

```bash
# Start containers
docker-compose up

# Stop containers
docker-compose down

# View logs
docker-compose logs -f app

# Execute command in container
docker-compose exec app dotnet ef database update

# Build and push image
docker build -t myregistry.azurecr.io/dotnet-config-server:latest .
docker push myregistry.azurecr.io/dotnet-config-server:latest
```

## Environment Variables

```bash
# Database
ConnectionStrings__DefaultConnection="Server=localhost;Database=ConfigDb;..."

# Logging
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft.AspNetCore=Warning

# Encryption
Encryption__Algorithm=AES256
Encryption__KeySize=256

# Webhooks
Webhook__MaxRetries=5
Webhook__TimeoutSeconds=30

# Rate Limiting
RateLimit__RequestsPerMinute=300

# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443
```

## Troubleshooting

### Check Server Health

```bash
curl https://localhost:5001/health
```

### View Application Logs

```bash
# Local
tail -f logs/application-*.txt

# Docker
docker-compose logs -f app

# Linux/Mac
cat logs/application-*.txt | tail -100
```

### Test Webhook Endpoint

```bash
curl -X POST https://your-webhook-endpoint \
  -H "Content-Type: application/json" \
  -H "X-Webhook-Signature: signature-here" \
  -d '{"configurationId":"...","eventType":"ConfigurationUpdated"}'
```

### Reset Database

```bash
# Using build script
./build.sh
make db-reset

# Using dotnet
dotnet ef database drop --force
dotnet ef database update
```

## Useful Links

- **API Documentation**: https://localhost:5001/swagger
- **Health Check**: https://localhost:5001/health
- **Full Documentation**: See `docs/` folder
- **Examples**: See `examples/` folder
- **Issues**: https://github.com/sarmkadan/dotnet-config-server/issues

## Getting Help

- Check [FAQ](./faq.md) for common questions
- See [Getting Started Guide](./getting-started.md) for detailed setup
- Read [Architecture Documentation](./architecture.md) for system design
- Open GitHub issue with detailed description

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**
