$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Info  { param($msg) Write-Host "[planner] $msg" -ForegroundColor Green }
function Warn  { param($msg) Write-Host "[planner] $msg" -ForegroundColor Yellow }
function Fail  { param($msg) Write-Host "[planner] $msg" -ForegroundColor Red; exit 1 }

# ── Prerequisites ──────────────────────────────────────────────────────────────
if (-not (Get-Command docker  -ErrorAction SilentlyContinue)) { Fail "Docker not found. Install Docker Desktop from https://docker.com" }
if (-not (Get-Command dotnet  -ErrorAction SilentlyContinue)) { Fail "dotnet SDK not found. Install from https://dot.net" }

# ── Start Docker services ──────────────────────────────────────────────────────
Info "Starting PostgreSQL..."
docker compose -f "$ScriptDir\docker-compose.yml" up -d

# ── Wait for PostgreSQL ────────────────────────────────────────────────────────
Info "Waiting for PostgreSQL to be ready..."
$retries = 20
do {
    Start-Sleep -Seconds 1
    docker compose -f "$ScriptDir\docker-compose.yml" exec -T postgres pg_isready -U planner -d planner_dev 2>$null | Out-Null
    $retries--
    if ($retries -le 0) { Fail "PostgreSQL did not become ready in time." }
} until ($LASTEXITCODE -eq 0)
Info "PostgreSQL is ready."

# ── Run the app ────────────────────────────────────────────────────────────────
Info "Starting Planner..."
Write-Host ""
Write-Host "  http://localhost:5000" -ForegroundColor Cyan
Write-Host "  Press Ctrl+C to stop." -ForegroundColor DarkGray
Write-Host ""

Set-Location $ScriptDir
dotnet run
