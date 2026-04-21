#!/bin/bash
set -e

VERSION=${1:-"4.0.0"}
ARCH=${2:-"arm64"}

# Locate dotnet — prefer the user's ~/.dotnet install, fall back to PATH
DOTNET="${DOTNET_ROOT:-$HOME/.dotnet}/dotnet"
if ! command -v "$DOTNET" &>/dev/null; then
  DOTNET="dotnet"
fi

# Always run from the repo root so relative paths resolve correctly
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR/.."

echo "Building GitExtensions Mac $VERSION for $ARCH..."

# Build the main app project (solution-level -o is not supported)
"$DOTNET" build src/app/GitUI.Avalonia/GitUI.Avalonia.csproj \
  -r "osx-$ARCH" \
  --self-contained true \
  -c Release \
  -p:Version=$VERSION

BUILD_OUTPUT="artifacts/Release/bin/GitUI.Avalonia/net10.0/osx-$ARCH"

APP_DIR="artifacts/GitExtensions.app"
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

cp -r "$BUILD_OUTPUT/." "$APP_DIR/Contents/MacOS/"
cp "src/app/GitUI.Avalonia/Info.plist" "$APP_DIR/Contents/Info.plist"

EXEC="$APP_DIR/Contents/MacOS/GitUI.Avalonia"
if [ -f "$EXEC" ]; then
  chmod +x "$EXEC"
  mv "$EXEC" "$APP_DIR/Contents/MacOS/GitExtensions"
fi

echo "App bundle created at $APP_DIR"
