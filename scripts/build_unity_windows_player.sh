#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_PROJECT="$ROOT_DIR/unity"
UNITY_BIN="${UNITY_BIN:-}"
VERSION="${VERSION:-0.0.1}"
BUILD_ROOT="${BUILD_ROOT:-$ROOT_DIR/release/unity-win}"
PLAYER_NAME="${PLAYER_NAME:-iBayeUnity}"
SKIP_PLUGIN=0

usage() {
    cat <<'USAGE'
Build Unity Windows player package.

Usage:
  ./scripts/build_unity_windows_player.sh [options]

Options:
  --unity-bin=<path>    Unity editor binary path (required if UNITY_BIN unset)
  --version=<ver>       package version (default: 0.0.1)
  --build-root=<path>   output root (default: release/unity-win)
  --player-name=<name>  executable base name (default: iBayeUnity)
  --skip-plugin         skip running build_unity_windows_plugin.sh
  -h, --help            show help

Output:
  <build-root>/<player-name>-windows-<version>.zip
USAGE
}

for arg in "$@"; do
    case "$arg" in
        --unity-bin=*) UNITY_BIN="${arg#*=}" ;;
        --version=*) VERSION="${arg#*=}" ;;
        --build-root=*) BUILD_ROOT="${arg#*=}" ;;
        --player-name=*) PLAYER_NAME="${arg#*=}" ;;
        --skip-plugin) SKIP_PLUGIN=1 ;;
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

resolve_unity_bin() {
    if [[ -n "$UNITY_BIN" && -x "$UNITY_BIN" ]]; then
        echo "$UNITY_BIN"
        return 0
    fi

    local candidates=(
        "$(command -v unity-editor 2>/dev/null || true)"
        "$(command -v Unity 2>/dev/null || true)"
        "/opt/Unity/Editor/Unity"
        "$HOME/Unity/Hub/Editor/2022.3.62f1/Editor/Unity"
    )

    local c
    for c in "${candidates[@]}"; do
        if [[ -n "$c" && -x "$c" ]]; then
            echo "$c"
            return 0
        fi
    done

    return 1
}

need_cmd zip

UNITY_BIN="$(resolve_unity_bin || true)"
if [[ -z "$UNITY_BIN" ]]; then
    echo "Unity editor binary not found. Set --unity-bin=/path/to/Unity or UNITY_BIN env." >&2
    exit 2
fi

if [[ "$SKIP_PLUGIN" -eq 0 ]]; then
    "$ROOT_DIR/scripts/build_unity_windows_plugin.sh"
fi

OUT_DIR="$BUILD_ROOT/$PLAYER_NAME-$VERSION"
rm -rf "$OUT_DIR"
mkdir -p "$OUT_DIR"

BUILD_EXE="$OUT_DIR/$PLAYER_NAME.exe"
LOG_DIR="$BUILD_ROOT/logs"
mkdir -p "$LOG_DIR"
SCENE_LOG="$LOG_DIR/unity-scene-$VERSION.log"
BUILD_LOG="$LOG_DIR/unity-build-$VERSION.log"

"$UNITY_BIN" -batchmode -quit \
    -projectPath "$UNITY_PROJECT" \
    -executeMethod IBayeSceneBuilder.CreateDefaultShellScene \
    -logFile "$SCENE_LOG"

IBAYE_BUILD_OUTPUT="$BUILD_EXE" \
"$UNITY_BIN" -batchmode -quit \
    -projectPath "$UNITY_PROJECT" \
    -executeMethod IBayeBuildPipeline.BuildWindowsPlayer \
    -logFile "$BUILD_LOG"

if [[ ! -f "$BUILD_EXE" ]]; then
    echo "Build finished but executable missing: $BUILD_EXE" >&2
    exit 1
fi

ZIP_PATH="$BUILD_ROOT/$PLAYER_NAME-windows-$VERSION.zip"
rm -f "$ZIP_PATH"
(
    cd "$OUT_DIR"
    zip -9 -r "$ZIP_PATH" .
)

echo "Windows player package created: $ZIP_PATH"
echo "Unity scene log: $SCENE_LOG"
echo "Unity build log: $BUILD_LOG"
