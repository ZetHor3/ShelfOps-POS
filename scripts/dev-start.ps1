# dev-start.ps1
# ShelfOps development environment launcher
# Starts PostgreSQL in WSL and the ASP.NET Core API

#Requires -Version 7.0

$ErrorActionPreference = "Stop"
$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Definition
$PROJECT_DIR = Split-Path -Parent $SCRIPT_DIR
$API_DIR = Join-Path $PROJECT_DIR "src" "ShelfOps.Api"

Write-Host "╔══════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║       ShelfOps Development Environment       ║" -ForegroundColor Cyan
Write-Host "╚══════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# ─── Step 1: Start PostgreSQL in WSL ───
Write-Host "[1/3] Starting PostgreSQL in WSL..." -ForegroundColor Yellow

$pgStatus = wsl sudo service postgresql status 2>&1
if ($LASTEXITCODE -ne 0 -or $pgStatus -match "not running") {
    Write-Host "  PostgreSQL is not running. Starting..." -ForegroundColor Gray
    wsl sudo service postgresql start 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ❌ Failed to start PostgreSQL." -ForegroundColor Red
        Write-Host "  Try: wsl sudo service postgresql start" -ForegroundColor Red
        exit 1
    }
    Write-Host "  ✅ PostgreSQL started." -ForegroundColor Green
} else {
    Write-Host "  ✅ PostgreSQL is already running." -ForegroundColor Green
}

# ─── Step 2: Verify database exists ───
Write-Host "[2/3] Verifying database..." -ForegroundColor Yellow

$dbCheck = wsl sudo -u postgres psql -t -c "SELECT 1 FROM pg_database WHERE datname='shelfops'" 2>&1
if ($dbCheck -match "1") {
    Write-Host "  ✅ Database 'shelfops' exists." -ForegroundColor Green
} else {
    Write-Host "  ⚠ Database 'shelfops' not found." -ForegroundColor Yellow
    Write-Host "  Creating database and user..."
    wsl sudo -u postgres psql -c "CREATE USER shelfops_user WITH PASSWORD 'postgres';" 2>&1 | Out-Null
    wsl sudo -u postgres psql -c "CREATE DATABASE shelfops OWNER shelfops_user;" 2>&1 | Out-Null
    wsl sudo -u postgres psql -c "GRANT ALL PRIVILEGES ON DATABASE shelfops TO shelfops_user;" 2>&1 | Out-Null
    Write-Host "  ✅ Database created." -ForegroundColor Green
}

# ─── Step 3: Start the API ───
Write-Host "[3/3] Starting ShelfOps API..." -ForegroundColor Yellow

Push-Location $API_DIR
try {
    # Check if dotnet is available
    $dotnetVersion = dotnet --version 2>&1
    Write-Host "  Using .NET $dotnetVersion" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  API will be available at: http://localhost:5000" -ForegroundColor White
    Write-Host "  Swagger UI at:            http://localhost:5000/swagger" -ForegroundColor White
    Write-Host "  Press Ctrl+C to stop." -ForegroundColor Gray
    Write-Host ""

    dotnet run
}
finally {
    Pop-Location
}