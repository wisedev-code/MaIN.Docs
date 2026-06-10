#!/usr/bin/env bash
# Downloads a MaIN.Docs generated artifact, extracts it, and runs it with `dotnet run`.
# Usage: curl -fsSL <host>/scripts/run-artifact.sh | bash -s -- "<artifactUrl>" "<archiveName>"
set -euo pipefail

CYAN='\033[1;36m'
MAGENTA='\033[1;35m'
YELLOW='\033[1;33m'
GREEN='\033[1;32m'
RESET='\033[0m'

echo ""
echo -e "${CYAN}  ╔═══════════════════════════════╗${RESET}"
echo -e "${CYAN}  ║      ${MAGENTA}MaIN ${CYAN}Package Runner      ║${RESET}"
echo -e "${CYAN}  ╚═══════════════════════════════╝${RESET}"
echo ""

URL="${1:?Missing artifact URL}"
NAME="${2:-artifact.zip}"
DIR="${NAME%.zip}"

echo -e "${YELLOW}==> Downloading $NAME${RESET}"
curl -fsSL "$URL" -o "$NAME"

echo -e "${YELLOW}==> Extracting to ./$DIR${RESET}"
rm -rf "$DIR"
mkdir -p "$DIR"
if command -v unzip >/dev/null 2>&1; then
  unzip -o -q "$NAME" -d "$DIR"
elif command -v python3 >/dev/null 2>&1; then
  python3 -c "import zipfile,sys; zipfile.ZipFile(sys.argv[1]).extractall(sys.argv[2])" "$NAME" "$DIR"
else
  echo "Error: need 'unzip' or 'python3' to extract the archive." >&2
  exit 1
fi

PROJECT_FILE="$(find "$DIR" -name '*.csproj' | head -n1)"
if [ -z "$PROJECT_FILE" ]; then
  echo "Error: no .csproj found in $DIR" >&2
  exit 1
fi
PROJECT_DIR="$(dirname "$PROJECT_FILE")"

RUN_ARGS=""
if grep -q 'Microsoft.NET.Sdk.Maui' "$PROJECT_FILE" 2>/dev/null; then
  echo -e "${YELLOW}==> MAUI project detected — checking workload...${RESET}"
  if ! dotnet workload list 2>/dev/null | grep -q 'maui'; then
    echo -e "${YELLOW}==> Installing MAUI workload (one-time setup)...${RESET}"
    dotnet workload install maui-maccatalyst
  fi
  RUN_ARGS="-f net9.0-maccatalyst"
fi

echo -e "${GREEN}==> Running 'dotnet run' in $PROJECT_DIR${RESET}"
echo ""
cd "$PROJECT_DIR"
dotnet run $RUN_ARGS
