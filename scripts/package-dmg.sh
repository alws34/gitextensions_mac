#!/bin/bash
set -e

VERSION=${1:-"4.0.0"}

if ! command -v create-dmg &>/dev/null; then
    echo "Installing create-dmg..."
    brew install create-dmg
fi

ICON_ARG=""
if [ -f "src/app/GitUI.Avalonia/Resources/AppIcon.icns" ]; then
  ICON_ARG="--volicon src/app/GitUI.Avalonia/Resources/AppIcon.icns"
fi

create-dmg \
  --volname "Git Extensions" \
  $ICON_ARG \
  --window-pos 200 120 \
  --window-size 600 400 \
  --icon-size 128 \
  --icon "GitExtensions.app" 150 200 \
  --hide-extension "GitExtensions.app" \
  --app-drop-link 450 200 \
  "artifacts/GitExtensions-$VERSION-mac.dmg" \
  "artifacts/GitExtensions.app"

echo "DMG created: artifacts/GitExtensions-$VERSION-mac.dmg"
