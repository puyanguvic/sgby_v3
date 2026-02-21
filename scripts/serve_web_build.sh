#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT_DIR="${OUT_DIR:-$ROOT_DIR/web/dist}"
PORT="${PORT:-8008}"
BUILD_IF_MISSING="${BUILD_IF_MISSING:-1}"

if [[ ! -f "$OUT_DIR/index.html" || ! -f "$OUT_DIR/baye.js" || ! -f "$OUT_DIR/baye.wasm" ]]; then
    if [[ "$BUILD_IF_MISSING" == "1" ]]; then
        "$ROOT_DIR/scripts/build_web_emscripten.sh"
    else
        echo "Missing web build outputs under $OUT_DIR" >&2
        exit 1
    fi
fi

echo "Serving $OUT_DIR at http://127.0.0.1:$PORT"
cd "$OUT_DIR"
python3 -m http.server "$PORT"
