#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
VERSION=""
TAG=""
TITLE=""
UNITY_BIN="${UNITY_BIN:-}"
PLAYER_NAME="${PLAYER_NAME:-iBayeUnity}"
BUILD_ROOT="${BUILD_ROOT:-$ROOT_DIR/release/unity-win}"

usage() {
    cat <<'USAGE'
Build Unity Windows player and publish to GitHub Release.

Usage:
  ./scripts/publish_unity_windows_release.sh --version=<ver> [options]

Options:
  --version=<ver>      required, release version (e.g. 0.2.0)
  --tag=<tag>          release tag (default: v<ver>)
  --title=<title>      release title (default: Unity Windows <ver>)
  --unity-bin=<path>   Unity editor binary path
  --player-name=<name> executable base name (default: iBayeUnity)
  --build-root=<path>  output root (default: release/unity-win)
  -h, --help           show help
USAGE
}

for arg in "$@"; do
    case "$arg" in
        --version=*) VERSION="${arg#*=}" ;;
        --tag=*) TAG="${arg#*=}" ;;
        --title=*) TITLE="${arg#*=}" ;;
        --unity-bin=*) UNITY_BIN="${arg#*=}" ;;
        --player-name=*) PLAYER_NAME="${arg#*=}" ;;
        --build-root=*) BUILD_ROOT="${arg#*=}" ;;
        -h|--help) usage; exit 0 ;;
        *)
            echo "Unknown arg: $arg" >&2
            usage
            exit 1
            ;;
    esac
done

if [[ -z "$VERSION" ]]; then
    echo "--version is required" >&2
    usage
    exit 1
fi

if [[ -z "$TAG" ]]; then
    TAG="v$VERSION"
fi
if [[ -z "$TITLE" ]]; then
    TITLE="Unity Windows $VERSION"
fi

if ! command -v gh >/dev/null 2>&1; then
    echo "Missing command: gh" >&2
    exit 1
fi

if ! gh auth status >/dev/null 2>&1; then
    echo "gh auth is not ready. Please run: gh auth login -h github.com" >&2
    exit 1
fi

build_args=(
    "--version=$VERSION"
    "--build-root=$BUILD_ROOT"
    "--player-name=$PLAYER_NAME"
)
if [[ -n "$UNITY_BIN" ]]; then
    build_args+=("--unity-bin=$UNITY_BIN")
fi

"$ROOT_DIR/scripts/build_unity_windows_player.sh" "${build_args[@]}"

ZIP_PATH="$BUILD_ROOT/$PLAYER_NAME-windows-$VERSION.zip"
if [[ ! -f "$ZIP_PATH" ]]; then
    echo "Missing package: $ZIP_PATH" >&2
    exit 1
fi

if ! gh release view "$TAG" >/dev/null 2>&1; then
    gh release create "$TAG" "$ZIP_PATH" \
        --title "$TITLE" \
        --notes "Unity migration preview build ($VERSION)."
else
    gh release upload "$TAG" "$ZIP_PATH" --clobber
fi

echo "Release published: $TAG"
echo "Asset: $ZIP_PATH"
