#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

VERSION=""
WITH_INSTALLER=0

usage() {
    cat <<'EOF'
Usage:
  ./scripts/preflight_windows_release.sh --version=1.0.5 [--with-installer]

What it does:
  1) Build Windows package locally on Ubuntu
  2) Validate release ZIP exists
  3) Validate key runtime files are present in ZIP
EOF
}

for arg in "$@"; do
    case "$arg" in
        --version=*)
            VERSION="${arg#*=}"
            ;;
        --with-installer)
            WITH_INSTALLER=1
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown argument: $arg" >&2
            usage
            exit 1
            ;;
    esac
done

if [[ -z "$VERSION" ]]; then
    echo "Missing --version" >&2
    usage
    exit 1
fi

if ! command -v unzip >/dev/null 2>&1; then
    echo "Missing command: unzip" >&2
    echo "Install with: sudo apt install -y unzip" >&2
    exit 1
fi

echo "== Local preflight for Windows release v$VERSION =="

if [[ "$WITH_INSTALLER" == "1" ]]; then
    ./build_windows_from_ubuntu.sh --version="$VERSION" --with-installer
else
    ./build_windows_from_ubuntu.sh --version="$VERSION"
fi

ZIP_FILE="release/iBaye-windows-portable-$VERSION.zip"
if [[ ! -f "$ZIP_FILE" ]]; then
    echo "Preflight failed: missing $ZIP_FILE" >&2
    exit 1
fi

TMP_LIST="$(mktemp)"
unzip -l "$ZIP_FILE" > "$TMP_LIST"

required_files=(
    "baye.exe"
    "dat.lib"
    "font.bin"
)

for f in "${required_files[@]}"; do
    if ! grep -qE "[[:space:]]${f}$" "$TMP_LIST"; then
        echo "Preflight failed: $f not found in $ZIP_FILE" >&2
        rm -f "$TMP_LIST"
        exit 1
    fi
done

rm -f "$TMP_LIST"

echo "Preflight passed."
echo "Ready to tag and publish: v$VERSION"
