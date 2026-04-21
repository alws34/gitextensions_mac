#!/bin/bash
set -e

VERSION=${1:-"4.0.0"}
ARCH=${2:-"arm64"}

echo "Building GitExtensions Mac $VERSION for $ARCH..."

dotnet publish GitExtensions.Mac.slnx \
  -r "osx-$ARCH" \
  --self-contained true \
  -c Release \
  -p:Version=$VERSION \
  -o "artifacts/publish/osx-$ARCH"

APP_DIR="artifacts/GitExtensions.app"
rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS"
mkdir -p "$APP_DIR/Contents/Resources"

cp -r "artifacts/publish/osx-$ARCH/." "$APP_DIR/Contents/MacOS/"
cp "src/app/GitUI.Avalonia/Info.plist" "$APP_DIR/Contents/Info.plist"

EXEC="$APP_DIR/Contents/MacOS/GitUI.Avalonia"
if [ -f "$EXEC" ]; then
  chmod +x "$EXEC"
  mv "$EXEC" "$APP_DIR/Contents/MacOS/GitExtensions"
fi

echo "App bundle created at $APP_DIR"
