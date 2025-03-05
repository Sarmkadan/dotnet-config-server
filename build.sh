#!/bin/bash

# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Functions
print_usage() {
    echo "Dotnet Config Server Build Script"
    echo ""
    echo "Usage: ./build.sh [command]"
    echo ""
    echo "Commands:"
    echo "  install      Install dependencies"
    echo "  build        Build the project"
    echo "  test         Run tests"
    echo "  clean        Clean build artifacts"
    echo "  run          Run locally"
    echo "  docker       Build Docker image"
    echo "  format       Format code"
    echo "  help         Show this help message"
    echo ""
}

print_step() {
    echo -e "${GREEN}==>${NC} $1"
}

print_error() {
    echo -e "${RED}Error:${NC} $1"
}

check_dotnet() {
    if ! command -v dotnet &> /dev/null; then
        print_error ".NET SDK is not installed"
        echo "Download from: https://dotnet.microsoft.com/download"
        exit 1
    fi

    local version=$(dotnet --version)
    print_step "Using .NET SDK version: $version"
}

build_install() {
    print_step "Installing dependencies..."
    check_dotnet
    dotnet restore
    print_step "Dependencies installed successfully"
}

build_build() {
    print_step "Building project..."
    check_dotnet
    dotnet build --configuration Release
    print_step "Build completed successfully"
}

build_test() {
    print_step "Running tests..."
    check_dotnet
    dotnet test --configuration Release --verbosity normal
    print_step "Tests completed successfully"
}

build_clean() {
    print_step "Cleaning build artifacts..."
    dotnet clean
    find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
    find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true
    print_step "Clean completed successfully"
}

build_run() {
    print_step "Running application..."
    check_dotnet

    # Check if database exists
    if ! dotnet ef database update 2>/dev/null; then
        print_error "Database migration failed"
        echo "Make sure SQL Server is running and connection string is correct"
        exit 1
    fi

    dotnet run
}

build_docker() {
    print_step "Building Docker image..."

    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed"
        echo "Download from: https://www.docker.com/products/docker-desktop"
        exit 1
    fi

    docker build -t dotnet-config-server:latest .
    print_step "Docker image built successfully"
    echo ""
    echo "To start the container:"
    echo "  docker-compose up"
}

build_format() {
    print_step "Formatting code..."
    check_dotnet

    # Install dotnet format if not already installed
    if ! dotnet format --version &> /dev/null; then
        dotnet tool install -g dotnet-format
    fi

    dotnet format
    print_step "Code formatting completed"
}

# Main script
case "${1:-help}" in
    install)
        build_install
        ;;
    build)
        build_install
        build_build
        ;;
    test)
        build_test
        ;;
    clean)
        build_clean
        ;;
    run)
        build_run
        ;;
    docker)
        build_docker
        ;;
    format)
        build_format
        ;;
    help)
        print_usage
        ;;
    *)
        print_error "Unknown command: $1"
        echo ""
        print_usage
        exit 1
        ;;
esac

exit 0
