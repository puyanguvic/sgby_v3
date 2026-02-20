#!/usr/bin/env bash
set -euo pipefail

BUILD_DIR="${BUILD_DIR:-build-win-mingw}"
DIST_DIR="${DIST_DIR:-dist-win}"
RELEASE_DIR="${RELEASE_DIR:-release}"
APP_VERSION="${APP_VERSION:-1.0.0}"
WITH_INSTALLER="${WITH_INSTALLER:-0}"

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

need_cmd() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Missing command: $1" >&2
        return 1
    fi
}

find_dll() {
    local dll="$1"
    local candidates=(
        "/usr/x86_64-w64-mingw32/lib"
        "/usr/lib/gcc/x86_64-w64-mingw32"
        "/usr/lib/gcc/x86_64-w64-mingw32/12-win32"
        "/usr/lib/gcc/x86_64-w64-mingw32/12-posix"
        "/usr/lib/gcc/x86_64-w64-mingw32/13-win32"
        "/usr/lib/gcc/x86_64-w64-mingw32/13-posix"
        "/usr/lib/gcc/x86_64-w64-mingw32/14-win32"
        "/usr/lib/gcc/x86_64-w64-mingw32/14-posix"
    )

    local dir
    for dir in "${candidates[@]}"; do
        if [[ -f "$dir/$dll" ]]; then
            echo "$dir/$dll"
            return 0
        fi
    done

    # Fallback scan for distros with non-standard mingw layout.
    local found
    found="$(find /usr -type f -name "$dll" 2>/dev/null | head -n 1 || true)"
    if [[ -n "$found" ]]; then
        echo "$found"
        return 0
    fi
    return 1
}

echo "== iBaye Windows build on Ubuntu =="

for arg in "$@"; do
    case "$arg" in
        --with-installer)
            WITH_INSTALLER=1
            ;;
        --without-installer)
            WITH_INSTALLER=0
            ;;
        --version=*)
            APP_VERSION="${arg#*=}"
            ;;
        -h|--help)
            cat <<'EOF'
Usage:
  ./build_windows_from_ubuntu.sh [--version=1.2.3] [--with-installer]

Default behavior:
  Build Windows portable ZIP only.

Optional:
  --with-installer   Also build NSIS installer (.exe).
EOF
            exit 0
            ;;
        *)
            echo "Unknown argument: $arg" >&2
            exit 1
            ;;
    esac
done

need_cmd cmake
need_cmd ninja
need_cmd x86_64-w64-mingw32-gcc
need_cmd x86_64-w64-mingw32-windres
need_cmd zip
if [[ "$WITH_INSTALLER" == "1" ]]; then
    need_cmd makensis
fi

echo "Preparing directories..."
rm -rf "$BUILD_DIR" "$DIST_DIR"
mkdir -p "$BUILD_DIR" "$DIST_DIR" "$RELEASE_DIR"

echo "Configuring CMake (MinGW-w64 cross toolchain)..."
cmake -S . -B "$BUILD_DIR" -G Ninja \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_SYSTEM_NAME=Windows \
    -DCMAKE_C_COMPILER=x86_64-w64-mingw32-gcc \
    -DCMAKE_RC_COMPILER=x86_64-w64-mingw32-windres

echo "Building..."
cmake --build "$BUILD_DIR" -j"$(nproc)"

EXE_PATH="$BUILD_DIR/src/baye.exe"
if [[ ! -f "$EXE_PATH" ]]; then
    echo "Build succeeded but executable not found: $EXE_PATH" >&2
    exit 1
fi

echo "Collecting runtime files..."
cp "$EXE_PATH" "$DIST_DIR/baye.exe"
cp "src/font.bin" "$DIST_DIR/font.bin"
if [[ -f "src/dat.lib" ]]; then
    cp "src/dat.lib" "$DIST_DIR/dat.lib"
elif [[ -f "src/dat.lib.orig" ]]; then
    cp "src/dat.lib.orig" "$DIST_DIR/dat.lib"
else
    echo "Missing src/dat.lib or src/dat.lib.orig" >&2
    exit 1
fi

# Optional font chunks, copied only when present.
cp src/font24.cn.* "$DIST_DIR/" 2>/dev/null || true
cp src/font24.en.* "$DIST_DIR/" 2>/dev/null || true

# MinGW runtime DLLs (best effort: copy when found).
for dll in libwinpthread-1.dll libgcc_s_seh-1.dll libstdc++-6.dll; do
    if path="$(find_dll "$dll")"; then
        cp "$path" "$DIST_DIR/$dll"
    fi
done

echo "Creating portable ZIP..."
(
    cd "$DIST_DIR"
    zip -9 -r "../$RELEASE_DIR/iBaye-windows-portable-$APP_VERSION.zip" .
)

if [[ "$WITH_INSTALLER" == "1" ]]; then
    echo "Building NSIS installer..."
    makensis \
        -DAPP_VERSION="$APP_VERSION" \
        -DINPUT_DIR="$DIST_DIR" \
        -DOUTPUT_DIR="$RELEASE_DIR" \
        installer.nsi
fi

echo ""
echo "Done."
echo "Portable package: $RELEASE_DIR/iBaye-windows-portable-$APP_VERSION.zip"
if [[ "$WITH_INSTALLER" == "1" ]]; then
    echo "Installer: $RELEASE_DIR/iBaye-Setup-$APP_VERSION.exe"
else
    echo "Installer: skipped (use --with-installer when needed)"
fi
