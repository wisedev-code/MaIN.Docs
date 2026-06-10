#!/usr/bin/env bash
# Downloads a MaIN.Docs generated artifact, extracts it, and runs it with `dotnet run`.
# Usage: curl -fsSL <host>/scripts/run-artifact.sh | bash -s -- "<artifactUrl>" "<archiveName>"
set -euo pipefail

URL="${1:?Missing artifact URL}"
NAME="${2:-artifact.zip}"
DIR="${NAME%.zip}"

echo "==> Downloading $NAME"
curl -fsSL "$URL" -o "$NAME"

echo "==> Extracting to ./$DIR"
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

echo "==> Running 'dotnet run' in $PROJECT_DIR"
cd "$PROJECT_DIR"
dotnet run
