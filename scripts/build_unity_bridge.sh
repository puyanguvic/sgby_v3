#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_DIR="${BUILD_DIR:-/tmp/iBaye-build-unity}"
BUILD_TYPE="${BUILD_TYPE:-Release}"
COPY_TO_UNITY=1

usage() {
    cat <<'USAGE'
Build iBaye native bridge for Unity.

Usage:
  ./scripts/build_unity_bridge.sh [options]

Options:
  --no-copy           build only, do not copy into unity/Assets/Plugins
  --build-dir=<path>  override CMake build dir
  --build-type=<type> CMake build type (default: Release)
  -h, --help          show this help
USAGE
}

for arg in "$@"; do
    case "$arg" in
        --no-copy) COPY_TO_UNITY=0 ;;
        --build-dir=*) BUILD_DIR="${arg#*=}" ;;
        --build-type=*) BUILD_TYPE="${arg#*=}" ;;
        -h|--help) usage; exit 0 ;;
        *)
            echo "Unknown arg: $arg" >&2
            usage
            exit 1
            ;;
    esac
done

cmake -S "$ROOT_DIR" -B "$BUILD_DIR" -G Ninja \
    -DCMAKE_BUILD_TYPE="$BUILD_TYPE" \
    -DBAYE_BUILD_UNITY_BRIDGE=ON
cmake --build "$BUILD_DIR" -j"$(nproc)"

if [[ "$COPY_TO_UNITY" -eq 0 ]]; then
    echo "Build complete: $BUILD_DIR"
    exit 0
fi

if [[ "$(uname -s)" == "Linux" ]]; then
    src_so="$BUILD_DIR/src/libibaye_unity_bridge.so"
    dst_so="$ROOT_DIR/unity/Assets/Plugins/Linux/x86_64/libibaye_unity_bridge.so"
    if [[ ! -f "$src_so" ]]; then
        echo "Missing bridge output: $src_so" >&2
        exit 1
    fi
    mkdir -p "$(dirname "$dst_so")"
    cp "$src_so" "$dst_so"
    echo "Copied: $dst_so"
else
    echo "Build completed. Copy plugin manually from: $BUILD_DIR/src"
fi
