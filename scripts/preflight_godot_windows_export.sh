#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

VERSION=""

usage() {
    cat <<'EOF'
用途:
  本地预检 Godot Windows 导出包完整性，避免发布后闪退。

用法:
  ./scripts/preflight_godot_windows_export.sh --version=<ver>
EOF
}

for arg in "$@"; do
    case "$arg" in
        --version=*)
            VERSION="${arg#*=}"
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

if ! command -v unzip >/dev/null 2>&1; then
    echo "缺少命令: unzip" >&2
    exit 1
fi

echo "== Godot Windows 导出预检: $VERSION =="

./scripts/build_godot_windows_export.sh --version="$VERSION"

ZIP_FILE="release/godot-win/iBaye-godot-windows-$VERSION.zip"
if [[ ! -f "$ZIP_FILE" ]]; then
    echo "预检失败: 缺少导出包 $ZIP_FILE" >&2
    exit 1
fi

TMP_LIST="$(mktemp)"
trap 'rm -f "$TMP_LIST"' EXIT
unzip -l "$ZIP_FILE" > "$TMP_LIST"

required_files=(
    "iBaye.exe"
    "ibaye_godot_bridge.dll"
    "libwinpthread-1.dll"
    "data_iBayeGodotShell_windows_x86_64/iBayeGodotShell.dll"
    "data_iBayeGodotShell_windows_x86_64/GodotSharp.dll"
    "data_iBayeGodotShell_windows_x86_64/iBayeGodotShell.runtimeconfig.json"
    "data_iBayeGodotShell_windows_x86_64/hostfxr.dll"
    "data_iBayeGodotShell_windows_x86_64/hostpolicy.dll"
    "data_iBayeGodotShell_windows_x86_64/coreclr.dll"
    "Run_With_Log.bat"
    "Diagnose_Runtime.bat"
    "FIRST_RUN_README.txt"
    "dat.lib"
    "font.bin"
    "font24.cn.1"
    "font24.cn.2"
    "font24.cn.3"
    "font24.cn.4"
    "font24.en.1"
    "font24.en.2"
)

for f in "${required_files[@]}"; do
    if ! grep -qE "[[:space:]]${f}$" "$TMP_LIST"; then
        echo "预检失败: 导出包缺少 $f" >&2
        exit 1
    fi
done

echo "预检通过。"
echo "导出包可用于发布: $ZIP_FILE"
