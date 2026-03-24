@REM =============================================================================
@REM Author: Vladyslav Zaiets | https://sarmkadan.com
@REM CTO & Software Architect
@REM =============================================================================

@echo off
setlocal enabledelayedexpansion

set DOTNET_VERSION=10.0.x

:main
if "%1"=="" (
    call :usage
    exit /b 0
)

if /i "%1"=="install" (
    call :install
    exit /b !ERRORLEVEL!
)

if /i "%1"=="build" (
    call :build
    exit /b !ERRORLEVEL!
)

if /i "%1"=="test" (
    call :test
    exit /b !ERRORLEVEL!
)

if /i "%1"=="clean" (
    call :clean
    exit /b !ERRORLEVEL!
)

if /i "%1"=="run" (
    call :run
    exit /b !ERRORLEVEL!
)

if /i "%1"=="docker" (
    call :docker
    exit /b !ERRORLEVEL!
)

if /i "%1"=="format" (
    call :format
    exit /b !ERRORLEVEL!
)

if /i "%1"=="help" (
    call :usage
    exit /b 0
)

echo Error: Unknown command '%1'
echo.
call :usage
exit /b 1

:usage
echo Dotnet Config Server Build Script
echo.
echo Usage: build.cmd [command]
echo.
echo Commands:
echo   install      Install dependencies
echo   build        Build the project
echo   test         Run tests
echo   clean        Clean build artifacts
echo   run          Run locally
echo   docker       Build Docker image
echo   format       Format code
echo   help         Show this help message
echo.
exit /b 0

:install
echo [*] Installing dependencies...
where dotnet >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    echo Error: .NET SDK is not installed
    echo Download from: https://dotnet.microsoft.com/download
    exit /b 1
)

dotnet --version
dotnet restore
echo [+] Dependencies installed successfully
exit /b 0

:build
echo [*] Building project...
where dotnet >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    echo Error: .NET SDK is not installed
    exit /b 1
)

dotnet build --configuration Release
if !ERRORLEVEL! NEQ 0 (
    echo Error: Build failed
    exit /b 1
)

echo [+] Build completed successfully
exit /b 0

:test
echo [*] Running tests...
where dotnet >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    echo Error: .NET SDK is not installed
    exit /b 1
)

dotnet test --configuration Release --verbosity normal
if !ERRORLEVEL! NEQ 0 (
    echo Error: Tests failed
    exit /b 1
)

echo [+] Tests completed successfully
exit /b 0

:clean
echo [*] Cleaning build artifacts...
dotnet clean
for /d /r . %%d in (bin,obj) do @if exist "%%d" rd /s /q "%%d"
echo [+] Clean completed successfully
exit /b 0

:run
echo [*] Running application...
where dotnet >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    echo Error: .NET SDK is not installed
    exit /b 1
)

echo [*] Applying database migrations...
dotnet ef database update
if !ERRORLEVEL! NEQ 0 (
    echo Error: Database migration failed
    echo Make sure SQL Server is running and connection string is correct
    exit /b 1
)

dotnet run
exit /b !ERRORLEVEL!

:docker
echo [*] Building Docker image...
where docker >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    echo Error: Docker is not installed
    echo Download from: https://www.docker.com/products/docker-desktop
    exit /b 1
)

docker build -t dotnet-config-server:latest .
if !ERRORLEVEL! NEQ 0 (
    echo Error: Docker build failed
    exit /b 1
)

echo [+] Docker image built successfully
echo.
echo To start the container:
echo   docker-compose up
exit /b 0

:format
echo [*] Formatting code...
where dotnet >nul 2>&1
if !ERRORLEVEL! NEQ 0 (
    echo Error: .NET SDK is not installed
    exit /b 1
)

dotnet tool list -g | find "dotnet-format" >nul
if !ERRORLEVEL! NEQ 0 (
    echo [*] Installing dotnet-format...
    dotnet tool install -g dotnet-format
)

dotnet format
echo [+] Code formatting completed
exit /b 0
