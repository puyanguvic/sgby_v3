#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_DIR="${BUILD_DIR:-$ROOT_DIR/build-web-emscripten}"
OUT_DIR="${OUT_DIR:-$ROOT_DIR/web/dist}"
BUILD_TYPE="${BUILD_TYPE:-Release}"
JOBS="${JOBS:-$(nproc)}"

need_cmd() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "Missing command: $1" >&2
        return 1
    fi
}

need_cmd emcmake
need_cmd cmake
need_cmd ninja

echo "== [1/3] Configure (Emscripten) =="
emcmake cmake -S "$ROOT_DIR" -B "$BUILD_DIR" -G Ninja \
    -DCMAKE_BUILD_TYPE="$BUILD_TYPE"

echo "== [2/3] Build =="
cmake --build "$BUILD_DIR" -j"$JOBS"

js_src="$(find "$BUILD_DIR" -type f -name 'baye.js' | head -n 1 || true)"
wasm_src="$(find "$BUILD_DIR" -type f -name 'baye.wasm' | head -n 1 || true)"

if [[ -z "$js_src" || -z "$wasm_src" ]]; then
    echo "Could not locate baye.js/baye.wasm in $BUILD_DIR" >&2
    exit 2
fi

echo "== [3/3] Assemble web output =="
mkdir -p "$OUT_DIR"
cp "$js_src" "$OUT_DIR/baye.js"
cp "$wasm_src" "$OUT_DIR/baye.wasm"

for opt in baye.data baye.worker.js baye.js.map baye.wasm.map; do
    opt_src="$(find "$BUILD_DIR" -type f -name "$opt" | head -n 1 || true)"
    if [[ -n "$opt_src" ]]; then
        cp "$opt_src" "$OUT_DIR/$opt"
    fi
done

cp "$ROOT_DIR/web/shell/index.html" "$OUT_DIR/index.html"
cp "$ROOT_DIR/web/shell/styles.css" "$OUT_DIR/styles.css"
cp "$ROOT_DIR/web/shell/app.js" "$OUT_DIR/app.js"

echo "Web build ready: $OUT_DIR"
echo "Run: ./scripts/serve_web_build.sh"
