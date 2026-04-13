@echo off
echo ========================================
echo ToyStore - Docker Deployment Script
echo ========================================
echo.

:: Check if Docker is running
docker info > nul 2>&1
if errorlevel 1 (
    echo [ERROR] Docker is not running. Please start Docker Desktop.
    pause
    exit /b 1
)

echo [INFO] Docker is running.
echo.

:: Parse command line arguments
set COMMAND=%1
if "%COMMAND%"=="" set COMMAND=up

if "%COMMAND%"=="up" goto :up
if "%COMMAND%"=="down" goto :down
if "%COMMAND%"=="build" goto :build
if "%COMMAND%"=="logs" goto :logs
if "%COMMAND%"=="dev" goto :dev
if "%COMMAND%"=="clean" goto :clean
goto :help

:up
echo [INFO] Starting all services...
docker-compose up -d
echo.
echo [SUCCESS] Services started!
echo API: http://localhost:5000
echo Swagger: http://localhost:5000/swagger
goto :end

:down
echo [INFO] Stopping all services...
docker-compose down
echo [SUCCESS] Services stopped!
goto :end

:build
echo [INFO] Building images...
docker-compose build --no-cache
echo [SUCCESS] Build completed!
goto :end

:logs
echo [INFO] Showing logs (Ctrl+C to exit)...
docker-compose logs -f
goto :end

:dev
echo [INFO] Starting development environment...
docker-compose -f docker-compose.dev.yml up -d
echo.
echo [SUCCESS] Development environment started!
echo SQL Server: localhost:1433
echo Redis: localhost:6379
echo.
echo Run your API locally with: dotnet run --project ToyStore.API
goto :end

:clean
echo [WARNING] This will remove all containers, volumes, and images!
set /p confirm="Are you sure? (y/N): "
if /i "%confirm%"=="y" (
    docker-compose down -v --rmi all
    echo [SUCCESS] Cleaned up!
) else (
    echo Cancelled.
)
goto :end

:help
echo.
echo Usage: deploy.bat [command]
echo.
echo Commands:
echo   up      - Start all services (default)
echo   down    - Stop all services
echo   build   - Rebuild all images
echo   logs    - View logs
echo   dev     - Start development environment (DB only)
echo   clean   - Remove all containers and images
echo.
goto :end

:end
echo.
pause
