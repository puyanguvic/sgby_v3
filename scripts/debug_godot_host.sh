#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_DIR="${BUILD_DIR:-build-debug-host}"
DAT_PATH="${DAT_PATH:-$ROOT_DIR/src/dat.lib.orig}"
FONT_DIR="${FONT_DIR:-$ROOT_DIR/src}"
DATA_DIR="${DATA_DIR:-$ROOT_DIR}"
MODE="${MODE:-init}"

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
    cat <<'EOF'
Usage:
  ./scripts/debug_godot_host.sh

Env vars:
  BUILD_DIR   CMake build dir (default: build-debug-host)
  DAT_PATH    dat.lib path (default: src/dat.lib.orig)
  FONT_DIR    font directory (default: src)
  DATA_DIR    save/data directory (default: repo root)
  MODE        init|engine (default: init)

Examples:
  ./scripts/debug_godot_host.sh
  MODE=engine DAT_PATH=/abs/path/dat.lib FONT_DIR=/abs/path ./scripts/debug_godot_host.sh
EOF
    exit 0
fi

cd "$ROOT_DIR"

cmake -S . -B "$BUILD_DIR" -G Ninja \
    -DCMAKE_BUILD_TYPE=Debug \
    -DBAYE_BUILD_DEBUG_HOST=ON

cmake --build "$BUILD_DIR" -j"$(nproc)" --target ibaye_debug_host

ARGS=(--dat "$DAT_PATH" --font-dir "$FONT_DIR" --data-dir "$DATA_DIR")
if [[ "$MODE" == "engine" ]]; then
    ARGS+=(--run-engine)
fi

"$BUILD_DIR/src/ibaye_debug_host" "${ARGS[@]}"
