# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

.PHONY: help install restore build test clean run docker-build docker-up docker-down db-migrate

# Variables
DOTNET := dotnet
PROJECT := dotnet-config-server
VERSION := 1.0.0
DOCKER_REGISTRY := myregistry.azurecr.io

help:
	@echo "Dotnet Config Server - Build Commands"
	@echo "======================================"
	@echo ""
	@echo "Development:"
	@echo "  make install         - Install dependencies"
	@echo "  make restore         - Restore NuGet packages"
	@echo "  make build           - Build the project"
	@echo "  make run             - Run locally (requires database)"
	@echo "  make test            - Run unit tests"
	@echo "  make clean           - Clean build artifacts"
	@echo ""
	@echo "Database:"
	@echo "  make db-migrate      - Apply database migrations"
	@echo "  make db-seed         - Seed database with test data"
	@echo "  make db-reset        - Reset database (drops and recreates)"
	@echo ""
	@echo "Docker:"
	@echo "  make docker-build    - Build Docker image"
	@echo "  make docker-up       - Start containers (docker-compose)"
	@echo "  make docker-down     - Stop containers"
	@echo "  make docker-push     - Push image to registry"
	@echo ""
	@echo "Code Quality:"
	@echo "  make lint            - Run code analysis"
	@echo "  make format          - Format code"
	@echo ""

install:
	@echo "Installing .NET SDK..."
	@command -v dotnet >/dev/null 2>&1 || (echo "Please install .NET SDK from https://dotnet.microsoft.com/download" && exit 1)
	@$(DOTNET) --version

restore:
	@echo "Restoring NuGet packages..."
	@$(DOTNET) restore

build: restore
	@echo "Building project..."
	@$(DOTNET) build --configuration Release

run:
	@echo "Running application..."
	@$(DOTNET) run

test: restore
	@echo "Running tests..."
	@$(DOTNET) test --configuration Release --verbosity normal

clean:
	@echo "Cleaning build artifacts..."
	@$(DOTNET) clean
	@find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
	@find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true

db-migrate:
	@echo "Applying database migrations..."
	@$(DOTNET) ef database update

db-seed:
	@echo "Seeding database..."
	@$(DOTNET) ef migrations add Seed --output-dir Data/Migrations 2>/dev/null || true
	@$(DOTNET) ef database update

db-reset:
	@echo "Resetting database..."
	@$(DOTNET) ef database drop --force 2>/dev/null || true
	@$(DOTNET) ef database update

docker-build:
	@echo "Building Docker image..."
	@docker build -t $(PROJECT):latest .
	@docker tag $(PROJECT):latest $(PROJECT):$(VERSION)
	@echo "Image built successfully: $(PROJECT):$(VERSION)"

docker-up:
	@echo "Starting containers..."
	@docker-compose up -d
	@echo "Containers started. Application available at http://localhost"
	@docker-compose ps

docker-down:
	@echo "Stopping containers..."
	@docker-compose down

docker-logs:
	@docker-compose logs -f app

docker-push:
	@echo "Pushing image to registry..."
	@docker tag $(PROJECT):$(VERSION) $(DOCKER_REGISTRY)/$(PROJECT):$(VERSION)
	@docker tag $(PROJECT):$(VERSION) $(DOCKER_REGISTRY)/$(PROJECT):latest
	@docker push $(DOCKER_REGISTRY)/$(PROJECT):$(VERSION)
	@docker push $(DOCKER_REGISTRY)/$(PROJECT):latest
	@echo "Image pushed successfully"

lint:
	@echo "Running code analysis..."
	@$(DOTNET) build --no-restore /p:TreatWarningsAsErrors=true

format:
	@echo "Formatting code..."
	@$(DOTNET) format --verify-no-changes --verbosity diagnostic || $(DOTNET) format

dev-setup:
	@echo "Setting up development environment..."
	@$(DOTNET) tool install -g dotnet-ef
	@$(DOTNET) tool install -g dotnet-format
	@$(DOTNET) restore
	@$(DOTNET) build
	@echo "Development environment ready!"

publish:
	@echo "Publishing application..."
	@$(DOTNET) publish -c Release -o ./publish

ci:
	@echo "Running CI pipeline..."
	@$(DOTNET) restore
	@$(DOTNET) build --configuration Release --no-restore
	@$(DOTNET) test --configuration Release --no-build --verbosity normal
	@echo "CI pipeline completed successfully"

all: clean restore build test
	@echo "Build completed successfully"

.DEFAULT_GOAL := help
