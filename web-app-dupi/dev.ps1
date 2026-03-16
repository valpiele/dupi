$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$AppSettings = "$ScriptDir\appsettings.Development.json"
$Example = "$ScriptDir\appsettings.Development.json.example"
$GoogleCreds = "$ScriptDir\google-credds"

function Info  { param($msg) Write-Host "[dupi] $msg" -ForegroundColor Green }
function Warn  { param($msg) Write-Host "[dupi] $msg" -ForegroundColor Yellow }
function Error { param($msg) Write-Host "[dupi] $msg" -ForegroundColor Red; exit 1 }

# ── Prerequisites ─────────────────────────────────────────────────────────────
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Error "Docker is not installed. Install Docker Desktop from https://docker.com"
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Error "dotnet SDK not found. Install from https://dot.net"
}

# ── appsettings.Development.json ─────────────────────────────────────────────
if (-not (Test-Path $AppSettings)) {
    Info "Creating appsettings.Development.json from example..."
    Copy-Item $Example $AppSettings

    # Auto-fill Google creds
    if (Test-Path $GoogleCreds) {
        $lines = Get-Content $GoogleCreds
        $clientId = $lines[0].Trim()
        $clientSecret = $lines[1].Trim()
        (Get-Content $AppSettings) `
            -replace "YOUR_GOOGLE_CLIENT_ID", $clientId `
            -replace "YOUR_GOOGLE_CLIENT_SECRET", $clientSecret |
            Set-Content $AppSettings
        Info "Google credentials loaded from google-credds"
    } else {
        Warn "No google-credds file found. Edit appsettings.Development.json manually."
    }

    # Prompt for Gemini API key
    Write-Host ""
    $geminiKey = Read-Host "Enter your Gemini API key"
    if ($geminiKey) {
        (Get-Content $AppSettings) -replace "YOUR_GEMINI_API_KEY", $geminiKey | Set-Content $AppSettings
        Info "Gemini API key saved."
    } else {
        Warn "No Gemini key entered. Add it to appsettings.Development.json later."
    }
}

# ── Start Docker services ─────────────────────────────────────────────────────
Info "Starting PostgreSQL + Azurite..."
docker compose -f "$ScriptDir\docker-compose.yml" up -d

# ── Wait for PostgreSQL ───────────────────────────────────────────────────────
Info "Waiting for PostgreSQL to be ready..."
$retries = 20
do {
    Start-Sleep -Seconds 1
    $ready = docker compose -f "$ScriptDir\docker-compose.yml" exec -T postgres pg_isready -U dupi -d dupi_dev 2>$null
    $retries--
    if ($retries -le 0) { Error "PostgreSQL did not become ready in time" }
} until ($LASTEXITCODE -eq 0)
Info "PostgreSQL is ready."

# ── Run the app ───────────────────────────────────────────────────────────────
Info "Starting dupi web app..."
Write-Host ""
Write-Host "  App will be available at: http://localhost:5000" -ForegroundColor Green
Write-Host "  Press Ctrl+C to stop." -ForegroundColor Green
Write-Host ""

Set-Location $ScriptDir
dotnet run
