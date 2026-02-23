#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

VERSION=""
TAG=""
SKIP_PREFLIGHT=0
CREATE_TAG=0
PUSH_TAG=0
DRAFT=0

usage() {
    cat <<'EOF'
用途:
  使用“本地导出产物”发布 Godot Windows 版本到 GitHub Release。

用法:
  ./scripts/publish_godot_windows_release.sh --version=<ver> [选项]

选项:
  --version=<ver>       版本号（例如 1.0.12）
  --tag=<tag>           Git tag（默认: v<version>）
  --skip-preflight      跳过本地预检
  --create-tag          若本地不存在 tag，则自动创建 annotated tag
  --push-tag            自动 push tag 到 origin
  --draft               以 draft 形式创建 release
  -h, --help            显示帮助

说明:
  1) 默认会先执行:
     ./scripts/preflight_godot_windows_export.sh --version=<ver>
  2) 默认产物:
     release/godot-win/iBaye-godot-windows-<ver>.zip
EOF
}

for arg in "$@"; do
    case "$arg" in
        --version=*)
            VERSION="${arg#*=}"
            ;;
        --tag=*)
            TAG="${arg#*=}"
            ;;
        --skip-preflight)
            SKIP_PREFLIGHT=1
            ;;
        --create-tag)
            CREATE_TAG=1
            ;;
        --push-tag)
            PUSH_TAG=1
            ;;
        --draft)
            DRAFT=1
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "未知参数: $arg" >&2
            usage
            exit 1
            ;;
    esac
done

if [[ -z "$VERSION" ]]; then
    echo "缺少 --version" >&2
    usage
    exit 1
fi

if ! command -v gh >/dev/null 2>&1; then
    echo "缺少命令: gh（GitHub CLI）" >&2
    exit 1
fi
if ! command -v sha256sum >/dev/null 2>&1; then
    echo "缺少命令: sha256sum" >&2
    exit 1
fi

if [[ -z "$TAG" ]]; then
    TAG="v$VERSION"
fi

ZIP_FILE="release/godot-win/iBaye-godot-windows-$VERSION.zip"
CHECKSUM_FILE="release/godot-win/SHA256SUMS-$VERSION.txt"

if [[ "$SKIP_PREFLIGHT" != "1" ]]; then
    ./scripts/preflight_godot_windows_export.sh --version="$VERSION"
fi

if [[ ! -f "$ZIP_FILE" ]]; then
    echo "找不到导出包: $ZIP_FILE" >&2
    exit 1
fi

(
    cd "$(dirname "$ZIP_FILE")"
    sha256sum "$(basename "$ZIP_FILE")" > "$(basename "$CHECKSUM_FILE")"
)

if ! git rev-parse -q --verify "refs/tags/$TAG" >/dev/null; then
    if [[ "$CREATE_TAG" == "1" ]]; then
        git tag -a "$TAG" -m "Release $TAG"
    else
        echo "本地不存在 tag: $TAG" >&2
        echo "可先执行: git tag -a $TAG -m \"Release $TAG\"" >&2
        echo "或增加参数: --create-tag" >&2
        exit 1
    fi
fi

if [[ "$PUSH_TAG" == "1" ]]; then
    git push origin "$TAG"
fi

if gh release view "$TAG" >/dev/null 2>&1; then
    gh release upload "$TAG" "$ZIP_FILE" "$CHECKSUM_FILE" --clobber
else
    if [[ "$DRAFT" == "1" ]]; then
        gh release create "$TAG" "$ZIP_FILE" "$CHECKSUM_FILE" --title "$TAG" --generate-notes --draft
    else
        gh release create "$TAG" "$ZIP_FILE" "$CHECKSUM_FILE" --title "$TAG" --generate-notes
    fi
fi

gh release view "$TAG" --json url | sed -n 's/.*"url":"\([^"]*\)".*/Release: \1/p'
echo "发布完成。"
