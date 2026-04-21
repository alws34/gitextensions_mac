#!/bin/bash
set -e

ICO=$(find src/app/GitUI -name "*.ico" | head -1)
if [ -z "$ICO" ]; then
  echo "No .ico found"
  exit 1
fi

mkdir -p artifacts src/app/GitUI.Avalonia/Resources

sips -s format png "$ICO" --out artifacts/icon.png 2>/dev/null || \
  convert "$ICO"[0] artifacts/icon.png

mkdir -p artifacts/AppIcon.iconset
for size in 16 32 64 128 256 512; do
    sips -z $size $size artifacts/icon.png \
         --out "artifacts/AppIcon.iconset/icon_${size}x${size}.png"
done
iconutil -c icns artifacts/AppIcon.iconset \
         -o src/app/GitUI.Avalonia/Resources/AppIcon.icns

echo "Created src/app/GitUI.Avalonia/Resources/AppIcon.icns"
