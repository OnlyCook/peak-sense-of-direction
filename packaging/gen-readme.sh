#!/usr/bin/env bash
#
# Generate the repo-root README.md from the packaged README plus the GitHub-only
# appendix, so the same content never has to be maintained in two places.
#
#   root README.md  =  packaging/README.md  +  packaging/README.github-extra.md
#
# Edit those two source files, then run this (build-release.sh runs it for you).
# The root README carries a "generated" banner and should not be edited by hand.
#
# Usage:  bash packaging/gen-readme.sh
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PKG="$REPO_ROOT/packaging"
SRC="$PKG/README.md"
EXTRA="$PKG/README.github-extra.md"
OUT="$REPO_ROOT/README.md"

for f in "$SRC" "$EXTRA"; do
  [[ -f "$f" ]] || { echo "ERROR: missing $f" >&2; exit 1; }
done

{
  echo "<!-- GENERATED FILE — do not edit by hand."
  echo "     Source: packaging/README.md + packaging/README.github-extra.md"
  echo "     Regenerate with: bash packaging/gen-readme.sh -->"
  echo
  cat "$SRC"
  echo
  cat "$EXTRA"
} > "$OUT"

echo "Wrote $OUT (from packaging/README.md + packaging/README.github-extra.md)"
