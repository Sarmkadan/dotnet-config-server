# Deployment Guide

This guide covers deploying Dotnet Config Server to production environments.

## Table of Contents

- [Pre-Deployment Checklist](#pre-deployment-checklist)
- [Docker Deployment](#docker-deployment)
- [Azure App Service](#azure-app-service)
- [Kubernetes](#kubernetes)
- [Production Configuration](#production-configuration)
- [Monitoring & Logging](#monitoring--logging)
- [Backup & Recovery](#backup--recovery)

## Pre-Deployment Checklist

Before deploying to production:

- [ ] Encryption keys generated and secured (Azure Key Vault, AWS Secrets Manager)
- [ ] Database backups configured
- [ ] SSL/TLS certificates obtained
- [ ] Connection string configured for production database
- [ ] Environment variables set correctly
- [ ] Rate limiting configured appropriately
- [ ] CORS policy configured for your clients
- [ ] Logging levels set (avoid Debug in production)
- [ ] Health check endpoints configured
- [ ] Monitoring and alerts set up
- [ ] Disaster recovery plan in place

## Docker Deployment

### Build Docker Image

```bash
# Clone repository
git clone https://github.com/sarmkadan/dotnet-config-server.git
cd dotnet-config-server

# Build image
docker build -t dotnet-config-server:1.0.0 .

# Tag for registry
docker tag dotnet-config-server:1.0.0 myregistry.azurecr.io/dotnet-config-server:1.0.0

# Push to registry
docker push myregistry.azurecr.io/dotnet-config-server:1.0.0
```

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS builder
WORKDIR /src
COPY ["dotnet-config-server.csproj", "."]
RUN dotnet restore "dotnet-config-server.csproj"
COPY . .
RUN dotnet build "dotnet-config-server.csproj" -c Release -o /app/build

FROM builder AS publish
RUN dotnet publish "dotnet-config-server.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "dotnet-config-server.dll"]
```

### Run Container

```bash
docker run -d \
  --name config-server \
  -p 80:8080 \
  -e ConnectionStrings__DefaultConnection="Server=sqlserver;Database=ConfigServer;User Id=sa;Password=MyPassword;" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e Logging__LogLevel__Default=Warning \
  -v /var/log/config-server:/app/logs \
  dotnet-config-server:1.0.0
```

### Docker Compose

```yaml
version: '3.8'

services:
  sql:
    image: mcr.microsoft.com/mssql/server:latest
    environment:
      SA_PASSWORD: "MyStrongPassword123!"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    volumes:
      - sql_data:/var/opt/mssql
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P "MyStrongPassword123!" -Q "SELECT 1"
      interval: 10s
      timeout: 3s
      retries: 5

  app:
    build: .
    ports:
      - "80:8080"
    environment:
      ConnectionStrings__DefaultConnection: "Server=sql;Database=ConfigServerDb;User Id=sa;Password=MyStrongPassword123!"
      ASPNETCORE_ENVIRONMENT: Production
      Logging__LogLevel__Default: Information
    depends_on:
      sql:
        condition: service_healthy
    volumes:
      - ./logs:/app/logs

volumes:
  sql_data:
```

Run with: `docker-compose up -d`

## Azure App Service

### Create Resources

```bash
# Variables
RESOURCE_GROUP="rg-config-server"
LOCATION="eastus"
APP_SERVICE_PLAN="asp-config-server"
WEB_APP="config-server-app"
SQL_SERVER="config-server-sql"
SQL_DATABASE="ConfigServerDb"

# Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# Create App Service Plan
az appservice plan create \
  --name $APP_SERVICE_PLAN \
  --resource-group $RESOURCE_GROUP \
  --sku B2 \
  --is-linux

# Create Web App
az webapp create \
  --resource-group $RESOURCE_GROUP \
  --plan $APP_SERVICE_PLAN \
  --name $WEB_APP \
  --runtime "DOTNET|10.0"

# Create SQL Server
az sql server create \
  --name $SQL_SERVER \
  --resource-group $RESOURCE_GROUP \
  --admin-user "sqladmin" \
  --admin-password "P@ssw0rd123!"

# Create SQL Database
az sql db create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name $SQL_DATABASE \
  --service-objective S1

# Configure firewall to allow Azure services
az sql server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name "AllowAzureServices" \
  --start-ip-address "0.0.0.0" \
  --end-ip-address "0.0.0.0"
```

### Configure Connection String

```bash
CONNECTION_STRING="Server=tcp:$SQL_SERVER.database.windows.net,1433;Initial Catalog=$SQL_DATABASE;Persist Security Info=False;User ID=sqladmin;Password=P@ssw0rd123!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config connection-string set \
  --resource-group $RESOURCE_GROUP \
  --name $WEB_APP \
  --settings DefaultConnection="$CONNECTION_STRING" \
  --connection-string-type SQLServer
```

### Deploy Application

```bash
# Option 1: Deploy from local git
cd dotnet-config-server
az webapp deployment source config-local-git \
  --resource-group $RESOURCE_GROUP \
  --name $WEB_APP

# Add remote and push
git remote add azure https://<deployment-user>@$WEB_APP.scm.azurewebsites.net/$WEB_APP.git
git push azure main

# Option 2: Deploy from Docker image
az webapp config container set \
  --resource-group $RESOURCE_GROUP \
  --name $WEB_APP \
  --docker-custom-image-name "myregistry.azurecr.io/dotnet-config-server:1.0.0" \
  --docker-registry-server-url "https://myregistry.azurecr.io" \
  --docker-registry-server-user "<username>" \
  --docker-registry-server-password "<password>"
```

### Configure Application Settings

```bash
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $WEB_APP \
  --settings \
  ASPNETCORE_ENVIRONMENT=Production \
  Logging__LogLevel__Default=Warning \
  Logging__LogLevel__Microsoft.AspNetCore=Error
```

## Kubernetes

### Create Namespace

```bash
kubectl create namespace config-server
```

### Create Secrets

```bash
# Database connection string
kubectl create secret generic db-connection \
  -n config-server \
  --from-literal=connection-string='Server=prod-db.example.com;Database=ConfigServerDb;User Id=sa;Password=YourPassword;'

# HMAC signing key for webhooks
kubectl create secret generic webhook-signing-key \
  -n config-server \
  --from-literal=signing-key='your-secure-random-key-here'

# Container registry credentials
kubectl create secret docker-registry acr-secret \
  -n config-server \
  --docker-server=myregistry.azurecr.io \
  --docker-username=<username> \
  --docker-password=<password> \
  --docker-email=user@example.com
```

### Kubernetes Deployment

```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: config-server
  namespace: config-server
spec:
  replicas: 3
  selector:
    matchLabels:
      app: config-server
  template:
    metadata:
      labels:
        app: config-server
    spec:
      imagePullSecrets:
        - name: acr-secret
      containers:
      - name: app
        image: myregistry.azurecr.io/dotnet-config-server:1.0.0
        ports:
        - containerPort: 8080
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: db-connection
              key: connection-string
        - name: Logging__LogLevel__Default
          value: "Information"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5
        volumeMounts:
        - name: logs
          mountPath: /app/logs
      volumes:
      - name: logs
        emptyDir: {}
---
apiVersion: v1
kind: Service
metadata:
  name: config-server-service
  namespace: config-server
spec:
  type: LoadBalancer
  ports:
  - port: 80
    targetPort: 8080
  selector:
    app: config-server
---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: config-server-hpa
  namespace: config-server
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: config-server
  minReplicas: 3
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

Deploy:
```bash
kubectl apply -f deployment.yaml
kubectl get pods -n config-server
```

## Production Configuration

### appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ApplicationSettings": {
    "EnableSwagger": false,
    "EnableDetailedErrors": false,
    "MaxVersionHistory": 100
  },
  "Encryption": {
    "KeySize": 256,
    "Iterations": 100000
  },
  "Webhook": {
    "MaxRetries": 5,
    "TimeoutSeconds": 30
  },
  "RateLimit": {
    "RequestsPerMinute": 300
  },
  "Cache": {
    "DefaultDurationSeconds": 300
  }
}
```

### Environment Variables (Production)

```bash
# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443

# Database
ConnectionStrings__DefaultConnection="Server=prod-db;Database=ConfigServerDb;..."

# Encryption
Encryption__Algorithm=AES256
Encryption__KeySize=256

# Logging
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft.AspNetCore=Warning

# Webhook
Webhook__MaxRetries=5
Webhook__TimeoutSeconds=30

# CORS
ApplicationSettings__EnableCors=true

# Rate Limiting
RateLimit__RequestsPerMinute=300

# SSL
ASPNETCORE_Kestrel__Certificates__Default__Path=/etc/secrets/tls/tls.crt
ASPNETCORE_Kestrel__Certificates__Default__KeyPath=/etc/secrets/tls/tls.key
```

## Monitoring & Logging

### Application Insights (Azure)

```csharp
// In Program.cs
builder.Services.AddApplicationInsightsTelemetry(
    builder.Configuration["APPINSIGHTS_CONNECTIONSTRING"]);
```

### Structured Logging

Logs include:
- Timestamp
- Level (Information, Warning, Error)
- Logger name
- Message
- Exception details

View logs:
```bash
# Local development
cat logs/application-*.txt

# Azure App Service
az webapp log tail --resource-group $RESOURCE_GROUP --name $WEB_APP

# Kubernetes
kubectl logs -n config-server -l app=config-server
```

### Health Check

Endpoint: `GET /health`

Response:
```json
{
  "status": "Healthy",
  "components": {
    "database": "Healthy",
    "cache": "Healthy"
  },
  "timestamp": "2026-05-04T10:30:00Z"
}
```

## Backup & Recovery

### Database Backups

```bash
# Azure SQL automatic backups
# Configured automatically, retention up to 35 days

# Manual backup
az sql db export \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name $SQL_DATABASE \
  --admin-user sqladmin \
  --admin-password "P@ssw0rd123!" \
  --storage-key "key" \
  --storage-key-type "StorageAccessKey" \
  --storage-uri "https://yourstg.blob.core.windows.net/backups/db.bacpac"

# Local backup
dotnet ef database dump --output backup.sql
```

### Recovery from Backup

```bash
# Restore from backup
az sql db import \
  --resource-group $RESOURCE_GROUP \
  --server $SQL_SERVER \
  --name $SQL_DATABASE \
  --admin-user sqladmin \
  --admin-password "P@ssw0rd123!" \
  --storage-key "key" \
  --storage-uri "https://yourstg.blob.core.windows.net/backups/db.bacpac"

# Verify recovery
kubectl port-forward -n config-server svc/config-server-service 8080:80
curl http://localhost:8080/health
```

### Disaster Recovery Plan

1. **Automated backups**: Daily at 2 AM UTC
2. **Backup retention**: 35 days minimum
3. **Recovery testing**: Monthly recovery drill
4. **RTO**: 4 hours (Recovery Time Objective)
5. **RPO**: 1 hour (Recovery Point Objective)

---

For questions or issues, refer to the [FAQ](./faq.md) or open a GitHub issue.
