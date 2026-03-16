#!/usr/bin/env bash
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
APPSETTINGS="$SCRIPT_DIR/appsettings.Development.json"
EXAMPLE="$SCRIPT_DIR/appsettings.Development.json.example"
GOOGLE_CREDS="$SCRIPT_DIR/google-credds"

# ── Colours ──────────────────────────────────────────────────────────────────
GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'
info()  { echo -e "${GREEN}[dupi]${NC} $1"; }
warn()  { echo -e "${YELLOW}[dupi]${NC} $1"; }
error() { echo -e "${RED}[dupi]${NC} $1"; exit 1; }

# ── Prerequisites ─────────────────────────────────────────────────────────────
command -v docker  &>/dev/null || error "Docker is not installed. Install Docker Desktop from https://docker.com"
command -v dotnet  &>/dev/null || error "dotnet SDK not found. Install from https://dot.net"

# ── appsettings.Development.json ─────────────────────────────────────────────
if [ ! -f "$APPSETTINGS" ]; then
  info "Creating appsettings.Development.json from example..."
  cp "$EXAMPLE" "$APPSETTINGS"

  # Auto-fill Google creds from google-credds file if it exists
  if [ -f "$GOOGLE_CREDS" ]; then
    CLIENT_ID=$(sed -n '1p' "$GOOGLE_CREDS" | tr -d '[:space:]')
    CLIENT_SECRET=$(sed -n '2p' "$GOOGLE_CREDS" | tr -d '[:space:]')
    if [[ "$OSTYPE" == "darwin"* ]]; then
      sed -i '' "s/YOUR_GOOGLE_CLIENT_ID/$CLIENT_ID/" "$APPSETTINGS"
      sed -i '' "s/YOUR_GOOGLE_CLIENT_SECRET/$CLIENT_SECRET/" "$APPSETTINGS"
    else
      sed -i "s/YOUR_GOOGLE_CLIENT_ID/$CLIENT_ID/" "$APPSETTINGS"
      sed -i "s/YOUR_GOOGLE_CLIENT_SECRET/$CLIENT_SECRET/" "$APPSETTINGS"
    fi
    info "Google credentials loaded from google-credds"
  else
    warn "No google-credds file found. Edit $APPSETTINGS and fill in Google credentials manually."
  fi

  # Prompt for Gemini API key
  echo ""
  read -p "$(echo -e "${YELLOW}Enter your Gemini API key:${NC} ")" GEMINI_KEY
  if [ -n "$GEMINI_KEY" ]; then
    if [[ "$OSTYPE" == "darwin"* ]]; then
      sed -i '' "s/YOUR_GEMINI_API_KEY/$GEMINI_KEY/" "$APPSETTINGS"
    else
      sed -i "s/YOUR_GEMINI_API_KEY/$GEMINI_KEY/" "$APPSETTINGS"
    fi
    info "Gemini API key saved."
  else
    warn "No Gemini key entered. Nutrition analysis won't work until you add it to appsettings.Development.json"
  fi
fi

# ── Start Docker services ─────────────────────────────────────────────────────
info "Starting PostgreSQL + Azurite..."
docker compose -f "$SCRIPT_DIR/docker-compose.yml" up -d

# ── Wait for PostgreSQL ───────────────────────────────────────────────────────
info "Waiting for PostgreSQL to be ready..."
RETRIES=20
until docker compose -f "$SCRIPT_DIR/docker-compose.yml" exec -T postgres pg_isready -U dupi -d dupi_dev &>/dev/null; do
  RETRIES=$((RETRIES - 1))
  [ $RETRIES -le 0 ] && error "PostgreSQL did not become ready in time"
  sleep 1
done
info "PostgreSQL is ready."

# ── Run the app ───────────────────────────────────────────────────────────────
info "Starting dupi web app..."
echo ""
echo -e "${GREEN}  App will be available at: http://localhost:5000${NC}"
echo -e "${GREEN}  Press Ctrl+C to stop.${NC}"
echo ""

cd "$SCRIPT_DIR"
dotnet run
