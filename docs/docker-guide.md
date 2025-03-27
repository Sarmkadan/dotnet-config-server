# Docker Guide
## Table of Contents
1. [Quick Start](#quick-start)
2. [Docker Compose](#docker-compose)
3. [Environment Variables](#environment-variables)
4. [Production Deployment](#production-deployment)
5. [Troubleshooting](#troubleshooting)
## Quick Start
To get started with Docker, follow these steps:
1. Install Docker on your system.
2. Pull the Dotnet Config Server image: `docker pull sarmkadan/dotnet-config-server`.
3. Run the container: `docker run -p 8080:8080 sarmkadan/dotnet-config-server`.
## Docker Compose
For a more complex setup, use Docker Compose.
1. Install Docker Compose on your system.
2. Create a `docker-compose.yml` file:
```yml
version: '3.8'
services:
  dotnet-config-server:
    image: sarmkadan/dotnet-config-server
    ports:
      - "8080:8080"
    depends_on:
      - mssql
    environment:
      - ConnectionStrings__DefaultConnection=Server=mssql;Database=DotnetConfigServerDb;...
  mssql:
    image: mcr.microsoft.com/mssql/server
    environment:
      - SA_PASSWORD=YourPassword123!
      - ACCEPT_EULA=Y
```
3. Run the containers: `docker-compose up`.
## Environment Variables
You can configure the container using environment variables.
* `ConnectionStrings__DefaultConnection`: The connection string to the database.
* `Encryption__KeySize`: The size of the encryption key.
* `Encryption__Algorithm`: The encryption algorithm to use.
## Production Deployment
For a production deployment, use a container orchestration tool like Kubernetes.
1. Create a Kubernetes deployment YAML file:
```yml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: dotnet-config-server
spec:
  replicas: 1
  selector:
    matchLabels:
      app: dotnet-config-server
  template:
    metadata:
      labels:
        app: dotnet-config-server
    spec:
      containers:
      - name: dotnet-config-server
        image: sarmkadan/dotnet-config-server
        ports:
        - containerPort: 8080
```
2. Apply the YAML file: `kubectl apply -f deployment.yaml`.
## Troubleshooting
If you encounter any issues, check the logs: `docker logs -f dotnet-config-server`.