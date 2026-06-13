#!/usr/bin/env bash
# Downloads a MaIN.Docs generated artifact, extracts it, and runs it with `dotnet run`.
# Usage: curl -fsSL <host>/scripts/run-artifact.sh | bash -s -- "<artifactUrl>" "<archiveName>"

RED='\033[1;31m'
CYAN='\033[1;36m'
MAGENTA='\033[1;35m'
YELLOW='\033[1;33m'
GREEN='\033[1;32m'
RESET='\033[0m'

_copy() {
  if command -v pbcopy   >/dev/null 2>&1; then printf '%s' "$1" | pbcopy;                       return
  elif command -v xclip  >/dev/null 2>&1; then printf '%s' "$1" | xclip -selection clipboard;   return
  elif command -v xsel   >/dev/null 2>&1; then printf '%s' "$1" | xsel --clipboard --input;      return
  fi
  return 1
}

_fail_build() {
  local output="$1"
  echo ""
  echo -e "${RED}  ✗ Build failed${RESET}"
  echo ""
  echo "$output" | grep -E "): error " | head -25
  echo ""
  local msg="The generated artifact failed to build. Please fix all the files:

$output"
  if _copy "$msg"; then
    echo -e "${CYAN}  ✓ Build errors copied to clipboard — paste into the chat to get it fixed.${RESET}"
  else
    echo -e "${YELLOW}  → Copy the errors above and paste into the MaIN.Docs chat to get them fixed.${RESET}"
  fi
  echo ""
  exit 1
}

_fail_run() {
  local code="$1" log="$2"
  echo ""
  echo -e "${RED}  ✗ Process exited with code $code${RESET}"
  echo ""
  local msg="The generated artifact crashed at runtime (exit code $code). Please fix the code:

$(tail -60 "$log")"
  if _copy "$msg"; then
    echo -e "${CYAN}  ✓ Error output copied to clipboard — paste into the chat to get it fixed.${RESET}"
  else
    echo -e "${YELLOW}  → Copy the output above and paste into the MaIN.Docs chat to get it fixed.${RESET}"
  fi
  echo ""
}

echo ""
echo -e "${CYAN}  ╔═══════════════════════════════╗${RESET}"
echo -e "${CYAN}  ║      ${MAGENTA}MaIN ${CYAN}Package Runner      ║${RESET}"
echo -e "${CYAN}  ╚═══════════════════════════════╝${RESET}"
echo ""

URL="${1:?Missing artifact URL}"
NAME="${2:-artifact.zip}"
DIR="${NAME%.zip}"

echo -e "${YELLOW}==> Downloading $NAME${RESET}"
curl -fsSL "$URL" -o "$NAME" || { echo -e "${RED}Download failed.${RESET}" >&2; exit 1; }

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
  echo -e "${RED}Error: no .csproj found in $DIR${RESET}" >&2
  exit 1
fi
PROJECT_DIR="$(dirname "$PROJECT_FILE")"
cd "$PROJECT_DIR"

# ── Build ──────────────────────────────────────────────────────────────────
echo -e "${YELLOW}==> Building project...${RESET}"
BUILD_OUT="$(dotnet build 2>&1)"
[ $? -eq 0 ] || _fail_build "$BUILD_OUT"
echo -e "${GREEN}  ✓ Build succeeded${RESET}"

# ── Run ───────────────────────────────────────────────────────────────────
echo -e "${GREEN}==> Running...${RESET}"
echo ""

RUN_LOG="$(mktemp /tmp/artifact-run.XXXXXX)"
dotnet run --no-build 2>&1 | tee "$RUN_LOG"
RUN_EXIT=${PIPESTATUS[0]}

# 0 = clean exit, 130 = Ctrl+C — both are fine
if [ "$RUN_EXIT" -ne 0 ] && [ "$RUN_EXIT" -ne 130 ]; then
  _fail_run "$RUN_EXIT" "$RUN_LOG"
fi
rm -f "$RUN_LOG"
