#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
GODOT_BIN="${GODOT_BIN:-godot-dotnet}"
GODOT_TEMPLATE_VERSION="${GODOT_TEMPLATE_VERSION:-}"
GODOT_RELEASE_TAG="${GODOT_RELEASE_TAG:-}"
DATA_HOME="${XDG_DATA_HOME:-$HOME/.local/share}"
TEMPLATES_ROOT="${TEMPLATES_ROOT:-$DATA_HOME/godot/export_templates}"

usage() {
    cat <<'EOF'
用途:
  安装与当前 Godot .NET 版本匹配的 Export Templates（Windows 导出必需）。

用法:
  ./scripts/install_godot_export_templates.sh [选项]

选项:
  --godot-bin=<bin>             Godot 可执行文件（默认: godot-dotnet）
  --template-version=<ver>      模板目录版本（如 4.6.1.stable.mono）
  --release-tag=<tag>           Godot release tag（如 4.6.1-stable）
  --templates-root=<path>       模板根目录（默认: $XDG_DATA_HOME/godot/export_templates）
  -h, --help                    显示帮助

说明:
  1) 未指定 --template-version 时，将通过 `<godot-bin> --version` 自动解析。
  2) 未指定 --release-tag 时，将从 template version 推导（去掉 `.mono` 并转为 x.y.z-stable）。
EOF
}

for arg in "$@"; do
    case "$arg" in
        --godot-bin=*)
            GODOT_BIN="${arg#*=}"
            ;;
        --template-version=*)
            GODOT_TEMPLATE_VERSION="${arg#*=}"
            ;;
        --release-tag=*)
            GODOT_RELEASE_TAG="${arg#*=}"
            ;;
        --templates-root=*)
            TEMPLATES_ROOT="${arg#*=}"
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

need_cmd() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "缺少命令: $1" >&2
        return 1
    fi
}

resolve_bin() {
    local candidate="$1"
    if [[ "$candidate" == */* ]]; then
        [[ -x "$candidate" ]] && { echo "$candidate"; return 0; }
        return 1
    fi
    command -v "$candidate" 2>/dev/null || return 1
}

derive_template_version() {
    local bin="$1"
    local raw
    raw="$("$bin" --version | head -n 1 | tr -d '\r\n')"
    if [[ -z "$raw" ]]; then
        return 1
    fi
    if [[ "$raw" == *".official."* ]]; then
        printf '%s\n' "${raw%%.official*}"
        return 0
    fi
    printf '%s\n' "$raw"
}

derive_release_tag() {
    local template_ver="$1"
    local base="${template_ver%.mono}"
    IFS='.' read -r major minor patch channel _rest <<< "$base"
    if [[ -z "${major:-}" || -z "${minor:-}" || -z "${patch:-}" || -z "${channel:-}" ]]; then
        return 1
    fi
    printf '%s.%s.%s-%s\n' "$major" "$minor" "$patch" "$channel"
}

BIN_PATH="$(resolve_bin "$GODOT_BIN" || true)"
if [[ -z "$BIN_PATH" ]]; then
    echo "找不到 Godot 可执行文件: $GODOT_BIN" >&2
    exit 1
fi

need_cmd curl
need_cmd unzip

if [[ -z "$GODOT_TEMPLATE_VERSION" ]]; then
    GODOT_TEMPLATE_VERSION="$(derive_template_version "$BIN_PATH")"
fi
if [[ -z "$GODOT_TEMPLATE_VERSION" ]]; then
    echo "无法解析 template version，请显式传入 --template-version。" >&2
    exit 1
fi

if [[ -z "$GODOT_RELEASE_TAG" ]]; then
    GODOT_RELEASE_TAG="$(derive_release_tag "$GODOT_TEMPLATE_VERSION" || true)"
fi
if [[ -z "$GODOT_RELEASE_TAG" ]]; then
    echo "无法从 $GODOT_TEMPLATE_VERSION 推导 release tag，请显式传入 --release-tag（示例: 4.6.1-stable）。" >&2
    exit 1
fi

install_dir="$TEMPLATES_ROOT/$GODOT_TEMPLATE_VERSION"

echo "== 安装 Godot Export Templates =="
echo "Godot Bin: $BIN_PATH"
echo "Template Version: $GODOT_TEMPLATE_VERSION"
echo "Release Tag: $GODOT_RELEASE_TAG"
echo "Install Dir: $install_dir"

tmp_dir="$(mktemp -d)"
trap 'rm -rf "$tmp_dir"' EXIT

asset_candidates=(
    "Godot_v${GODOT_RELEASE_TAG}_mono_export_templates.tpz"
    "Godot_v${GODOT_RELEASE_TAG}_dotnet_export_templates.tpz"
    "Godot_v${GODOT_RELEASE_TAG}_export_templates.tpz"
)

download_ok=0
asset_name=""
tpz=""
asset_url=""
for candidate in "${asset_candidates[@]}"; do
    candidate_url="https://github.com/godotengine/godot/releases/download/${GODOT_RELEASE_TAG}/${candidate}"
    candidate_path="$tmp_dir/$candidate"
    echo "尝试下载模板包: $candidate"
    if curl -fL "$candidate_url" -o "$candidate_path"; then
        download_ok=1
        asset_name="$candidate"
        tpz="$candidate_path"
        asset_url="$candidate_url"
        break
    fi
done

if [[ "$download_ok" != "1" ]]; then
    echo "下载模板失败：尝试了以下资产名但都不可用：" >&2
    printf '  - %s\n' "${asset_candidates[@]}" >&2
    exit 1
fi
echo "使用模板资产: $asset_name"

echo "解压模板..."
unzip -q "$tpz" -d "$tmp_dir/unpacked"

template_payload_dir="$(dirname "$(find "$tmp_dir/unpacked" -type f -name 'windows_release_x86_64.exe' | head -n 1 || true)")"
if [[ -z "$template_payload_dir" || ! -d "$template_payload_dir" ]]; then
    echo "模板包内未找到 windows_release_x86_64.exe，下载内容可能不匹配: $asset_url" >&2
    exit 1
fi

rm -rf "$install_dir"
mkdir -p "$install_dir"
cp -a "$template_payload_dir/." "$install_dir/"

if [[ ! -f "$install_dir/windows_debug_x86_64.exe" || ! -f "$install_dir/windows_release_x86_64.exe" ]]; then
    echo "模板安装不完整，缺少 Windows 导出模板文件。" >&2
    exit 1
fi

if ! strings "$install_dir/windows_release_x86_64.exe" | grep -q "CSharpScript"; then
    echo "警告：模板似乎不包含 C# loader（未检测到 CSharpScript）。" >&2
    echo "这会导致运行时报: No loader found for resource: res://*.cs" >&2
    echo "请确认下载了 .NET/mono 导出模板，而不是标准模板。" >&2
fi

echo "安装完成。"
echo "可用模板: $install_dir"
