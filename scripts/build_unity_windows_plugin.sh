#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_DIR="${BUILD_DIR:-/tmp/iBaye-build-unity-win}"
OUT_DIR="${OUT_DIR:-$ROOT_DIR/unity/Assets/Plugins/x86_64}"
BUILD_TYPE="${BUILD_TYPE:-Release}"
COPY_DEPS=1

usage() {
    cat <<'USAGE'
Build Windows native plugin for Unity (cross-compile from Ubuntu).

Usage:
  ./scripts/build_unity_windows_plugin.sh [options]

Options:
  --build-dir=<path>   CMake build dir (default: /tmp/iBaye-build-unity-win)
  --out-dir=<path>     Plugin output dir (default: unity/Assets/Plugins/x86_64)
  --build-type=<type>  CMake build type (default: Release)
  --no-deps            Copy only ibaye_unity_bridge.dll (skip MinGW runtime DLLs)
  -h, --help           Show this help
USAGE
}

for arg in "$@"; do
    case "$arg" in
        --build-dir=*) BUILD_DIR="${arg#*=}" ;;
        --out-dir=*) OUT_DIR="${arg#*=}" ;;
        --build-type=*) BUILD_TYPE="${arg#*=}" ;;
        --no-deps) COPY_DEPS=0 ;;
        -h|--help) usage; exit 0 ;;
        *)
            echo "Unknown arg: $arg" >&2
            usage
            exit 1
            ;;
    esac
done

need_cmd() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Missing command: $1" >&2
        return 1
    fi
}

find_mingw_dll() {
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

    local found
    found="$(find /usr -type f -name "$dll" 2>/dev/null | head -n 1 || true)"
    if [[ -n "$found" ]]; then
        echo "$found"
        return 0
    fi

    return 1
}

need_cmd cmake
need_cmd ninja
need_cmd x86_64-w64-mingw32-gcc
need_cmd x86_64-w64-mingw32-windres
need_cmd x86_64-w64-mingw32-objdump

cmake -S "$ROOT_DIR" -B "$BUILD_DIR" -G Ninja \
    -DCMAKE_BUILD_TYPE="$BUILD_TYPE" \
    -DCMAKE_SYSTEM_NAME=Windows \
    -DCMAKE_C_COMPILER=x86_64-w64-mingw32-gcc \
    -DCMAKE_RC_COMPILER=x86_64-w64-mingw32-windres \
    -DBAYE_BUILD_UNITY_BRIDGE=ON
cmake --build "$BUILD_DIR" -j"$(nproc)"

bridge_dll="$BUILD_DIR/src/ibaye_unity_bridge.dll"
if [[ ! -f "$bridge_dll" ]]; then
    echo "Missing bridge DLL: $bridge_dll" >&2
    exit 1
fi

mkdir -p "$OUT_DIR"
cp "$bridge_dll" "$OUT_DIR/ibaye_unity_bridge.dll"

echo "Copied: $OUT_DIR/ibaye_unity_bridge.dll"

if [[ "$COPY_DEPS" -eq 0 ]]; then
    echo "Skipped MinGW runtime dependency copy (--no-deps)."
    exit 0
fi

mapfile -t dll_deps < <(
    x86_64-w64-mingw32-objdump -p "$bridge_dll" \
        | awk '/DLL Name:/ {print $3}' \
        | sort -u
)

for dll in "${dll_deps[@]}"; do
    case "${dll,,}" in
        kernel32.dll|user32.dll|gdi32.dll|msvcrt.dll|advapi32.dll|shell32.dll|ole32.dll|comdlg32.dll|ws2_32.dll)
            continue
            ;;
    esac

    if path="$(find_mingw_dll "$dll")"; then
        cp "$path" "$OUT_DIR/$dll"
        echo "Copied dependency: $OUT_DIR/$dll"
    else
        echo "Missing runtime DLL dependency: $dll" >&2
        exit 1
    fi
done
