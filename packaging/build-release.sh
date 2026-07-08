#!/usr/bin/env bash
#
# Assemble a Thunderstore-ready release zip.
#
# manifest.json's version_number is the ONLY place to bump the version, this
# script syncs it into SenseOfDirection.csproj and PluginInfo.cs before
# building (CHANGELOG.md stays hand-maintained, this only warns if the new
# version has no entry yet).
#
# Output: dist/SenseOfDirection-<version>.zip with everything at the zip ROOT:
#   manifest.json
#   icon.png            (256x256)
#   README.md
#   CHANGELOG.md
#   LICENSE             (if present)
#   SenseOfDirection.dll
#
# r2modman installs the whole package into BepInEx/plugins/<Team>-SenseOfDirection/,
# so a root-level DLL lands correctly and BepInEx loads it.
#
# Usage:  bash packaging/build-release.sh
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PKG="$REPO_ROOT/packaging"
PROJ="$REPO_ROOT/src/SenseOfDirection"
DIST="$REPO_ROOT/dist"

# Version comes from manifest.json (single source of truth for the package).
# Bump it there ONLY, everything below mirrors it, nothing else should ever be
# hand-edited to a new version number.
VERSION="$(grep -oE '"version_number"[[:space:]]*:[[:space:]]*"[^"]+"' "$PKG/manifest.json" | grep -oE '[0-9]+\.[0-9]+\.[0-9]+')"
if [[ -z "$VERSION" ]]; then echo "ERROR: could not read version_number from manifest.json" >&2; exit 1; fi
echo "Packaging SenseOfDirection v$VERSION"

# 0. Sync the version into every other place it's declared.
echo "Syncing version $VERSION into csproj / PluginInfo.cs..."

sed -i -E "s#(<Version>)[0-9]+\.[0-9]+\.[0-9]+(</Version>)#\1$VERSION\2#" \
  "$PROJ/SenseOfDirection.csproj"

sed -i -E "s#(public const string Version = \")[0-9]+\.[0-9]+\.[0-9]+(\";)#\1$VERSION\2#" \
  "$PROJ/PluginInfo.cs"

# CHANGELOG.md is hand-maintained (one heading per release, old entries must
# never be touched), so this is just a nudge, not an auto-edit.
if ! grep -q "^## $VERSION" "$PKG/CHANGELOG.md"; then
  echo "WARNING: packaging/CHANGELOG.md has no '## $VERSION' entry yet, add one before publishing." >&2
fi

# 0.5. Keep the repo-root README.md in sync with the packaged README (single source).
bash "$PKG/gen-readme.sh"

# 1. Build the DLL (Release).
echo "Building..."
dotnet build "$PROJ/SenseOfDirection.csproj" -c Release >/dev/null
DLL="$PROJ/bin/Release/SenseOfDirection.dll"
[[ -f "$DLL" ]] || { echo "ERROR: build output not found: $DLL" >&2; exit 1; }

# 2. Validate the icon is exactly 256x256 (Thunderstore requirement).
if command -v python3 >/dev/null 2>&1; then
  python3 - "$PKG/icon.png" <<'PY'
import sys
from PIL import Image
w,h = Image.open(sys.argv[1]).size
assert (w,h)==(256,256), f"icon.png must be 256x256, got {w}x{h}"
PY
fi

# 3. Stage.
STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT
cp "$PKG/manifest.json" "$PKG/icon.png" "$PKG/README.md" "$PKG/CHANGELOG.md" "$STAGE/"
[[ -f "$REPO_ROOT/LICENSE" ]] && cp "$REPO_ROOT/LICENSE" "$STAGE/LICENSE" || echo "NOTE: no LICENSE file yet (pick one before publishing)."
cp "$DLL" "$STAGE/SenseOfDirection.dll"

# 4. Zip (files at the root of the archive).
mkdir -p "$DIST"
OUT="$DIST/SenseOfDirection-$VERSION.zip"
rm -f "$OUT"
( cd "$STAGE" && zip -r -q "$OUT" . )
echo "Wrote $OUT"
unzip -l "$OUT"
