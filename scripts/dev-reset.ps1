# dev-reset.ps1
# ShelfOps database reset script
# Drops and recreates the database, runs seed data

#Requires -Version 7.0

$ErrorActionPreference = "Stop"
$SCRIPT_DIR = Split-Path -Parent $MyInvocation.MyCommand.Definition
$PROJECT_DIR = Split-Path -Parent $SCRIPT_DIR
$API_DIR = Join-Path $PROJECT_DIR "src" "ShelfOps.Api"

Write-Host "╔══════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║         ShelfOps Database Reset Tool         ║" -ForegroundColor Magenta
Write-Host "╚══════════════════════════════════════════════╝" -ForegroundColor Magenta
Write-Host ""

# ─── Confirmation ───
$confirm = Read-Host "This will DROP and recreate the 'shelfops' database. Continue? (y/N)"
if ($confirm -notmatch '^[Yy]') {
    Write-Host "Operation cancelled." -ForegroundColor Gray
    exit 0
}

# ─── Step 1: Ensure PostgreSQL is running ───
Write-Host "[1/4] Ensuring PostgreSQL is running..." -ForegroundColor Yellow

$pgStatus = wsl sudo service postgresql status 2>&1
if ($LASTEXITCODE -ne 0 -or $pgStatus -match "not running") {
    Write-Host "  Starting PostgreSQL..." -ForegroundColor Gray
    wsl sudo service postgresql start 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ❌ Failed to start PostgreSQL." -ForegroundColor Red
        exit 1
    }
}
Write-Host "  ✅ PostgreSQL is running." -ForegroundColor Green

# ─── Step 2: Drop and recreate database ───
Write-Host "[2/4] Dropping and recreating database..." -ForegroundColor Yellow

# Terminate existing connections
wsl sudo -u postgres psql -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='shelfops' AND pid <> pg_backend_pid();" 2>&1 | Out-Null

# Drop the database
wsl sudo -u postgres psql -c "DROP DATABASE IF EXISTS shelfops WITH (FORCE);" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "  ❌ Failed to drop database. Try stopping the API first." -ForegroundColor Red
    exit 1
}

# Recreate
wsl sudo -u postgres psql -c "CREATE DATABASE shelfops OWNER shelfops_user;" 2>&1
Write-Host "  ✅ Database recreated." -ForegroundColor Green

# ─── Step 3: Apply EF migrations ───
Write-Host "[3/4] Applying EF Core migrations..." -ForegroundColor Yellow

Push-Location $API_DIR
try {
    dotnet ef database update `
        --project (Join-Path $PROJECT_DIR "src" "ShelfOps.Infrastructure") `
        --startup-project $API_DIR 2>&1

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  ⚠ Migrations failed. The API will auto-create the database on first run." -ForegroundColor Yellow
    } else {
        Write-Host "  ✅ Migrations applied." -ForegroundColor Green
    }
}
finally {
    Pop-Location
}

# ─── Step 4: Verify ───
Write-Host "[4/4] Verifying..." -ForegroundColor Yellow

$tableCount = wsl sudo -u postgres psql -d shelfops -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public';" 2>&1
Write-Host "  Tables in database: $($tableCount.Trim())" -ForegroundColor Gray

Write-Host ""
Write-Host "✅ Database reset complete." -ForegroundColor Green
Write-Host "  Run 'scripts\dev-start.ps1' to start the API." -ForegroundColor White