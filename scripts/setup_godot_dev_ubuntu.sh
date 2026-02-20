#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_DIR="${BUILD_DIR:-/tmp/iBaye-build-godot}"
GODOT_INSTALL_DIR="${GODOT_INSTALL_DIR:-$HOME/.local/opt/godot-dotnet}"
GODOT_BIN_LINK="${GODOT_BIN_LINK:-$HOME/.local/bin/godot-dotnet}"
GODOT_RELEASE_TAG="${GODOT_RELEASE_TAG:-}"

RUN_AFTER_SETUP=0
SKIP_APT=0
SKIP_DOTNET=0
SKIP_GODOT=0
SKIP_BRIDGE=0

usage() {
    cat <<'EOF'
用途:
  Ubuntu 一键安装 Godot(.NET) + dotnet SDK，并编译 iBaye Godot bridge。

用法:
  ./scripts/setup_godot_dev_ubuntu.sh [选项]

选项:
  --run                 安装完成后直接启动 Godot 项目
  --skip-apt            跳过 apt 依赖安装
  --skip-dotnet         跳过 dotnet SDK 安装
  --skip-godot          跳过 Godot .NET 下载
  --skip-bridge         跳过桥接库编译
  --godot-tag=<tag>     指定 Godot 发布标签(如 4.2.2-stable)
  -h, --help            显示帮助

环境变量:
  BUILD_DIR             CMake 构建目录 (默认 /tmp/iBaye-build-godot)
  GODOT_INSTALL_DIR     Godot 解压目录 (默认 ~/.local/opt/godot-dotnet)
  GODOT_BIN_LINK        命令软链路径 (默认 ~/.local/bin/godot-dotnet)
  GODOT_RELEASE_TAG     同 --godot-tag
EOF
}

for arg in "$@"; do
    case "$arg" in
        --run) RUN_AFTER_SETUP=1 ;;
        --skip-apt) SKIP_APT=1 ;;
        --skip-dotnet) SKIP_DOTNET=1 ;;
        --skip-godot) SKIP_GODOT=1 ;;
        --skip-bridge) SKIP_BRIDGE=1 ;;
        --godot-tag=*) GODOT_RELEASE_TAG="${arg#*=}" ;;
        -h|--help) usage; exit 0 ;;
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

if [[ ! -f /etc/os-release ]]; then
    echo "仅支持 Ubuntu 环境: /etc/os-release 不存在" >&2
    exit 1
fi

# shellcheck disable=SC1091
source /etc/os-release
if [[ "${ID:-}" != "ubuntu" ]]; then
    echo "当前系统不是 Ubuntu (ID=${ID:-unknown})，脚本停止。" >&2
    exit 1
fi

SUDO=(sudo)
if [[ "${EUID:-$(id -u)}" -eq 0 ]]; then
    SUDO=()
fi

echo "== iBaye Godot 开发环境初始化 =="
echo "ROOT_DIR=$ROOT_DIR"

if [[ "$SKIP_APT" -eq 0 ]]; then
    echo "-> 安装基础依赖 (curl/unzip/python3/gnupg/cmake/ninja-build)..."
    "${SUDO[@]}" apt-get update
    "${SUDO[@]}" apt-get install -y \
        ca-certificates \
        curl \
        gnupg \
        unzip \
        python3 \
        cmake \
        ninja-build
fi

need_cmd curl
need_cmd unzip
need_cmd python3
need_cmd cmake
need_cmd ninja

install_dotnet_sdk() {
    if command -v dotnet >/dev/null 2>&1; then
        if dotnet --list-sdks 2>/dev/null | grep -q '^8\.'; then
            echo "-> dotnet SDK 8 已安装，跳过。"
            return 0
        fi
    fi

    echo "-> 安装 dotnet SDK 8..."
    local deb="/tmp/packages-microsoft-prod.deb"
    local version_id="${VERSION_ID:-}"
    if [[ -z "$version_id" ]]; then
        echo "无法识别 Ubuntu VERSION_ID，停止。" >&2
        return 1
    fi

    curl -fsSL "https://packages.microsoft.com/config/ubuntu/${version_id}/packages-microsoft-prod.deb" -o "$deb"
    "${SUDO[@]}" dpkg -i "$deb"
    rm -f "$deb"

    "${SUDO[@]}" apt-get update
    "${SUDO[@]}" apt-get install -y dotnet-sdk-8.0
}

resolve_godot_asset() {
    python3 - "$GODOT_RELEASE_TAG" <<'PY'
import json
import re
import sys
import urllib.request
from urllib.error import HTTPError

tag = sys.argv[1].strip()
api_base = "https://api.github.com/repos/godotengine/godot/releases"

patterns = [
    r"_mono_linux_x86_64\.zip$",
    r"_mono_linux\.x86_64\.zip$",
    r"_dotnet_linux_x86_64\.zip$",
    r"_dotnet_linux\.x86_64\.zip$",
]
compiled = [re.compile(p) for p in patterns]

def pick_asset(release):
    for asset in release.get("assets", []):
        name = asset.get("name", "")
        if "console" in name.lower():
            continue
        for pat in compiled:
            if pat.search(name):
                return release.get("tag_name", ""), name, asset.get("browser_download_url", "")
    return None

if tag:
    url = f"{api_base}/tags/{tag}"
    try:
        with urllib.request.urlopen(url, timeout=30) as resp:
            release = json.load(resp)
    except HTTPError as e:
        print(f"ERROR: 无法获取指定 Godot tag: {tag} ({e.code})")
        sys.exit(2)
    picked = pick_asset(release)
    if picked is None:
        print(f"ERROR: 指定 tag {tag} 没找到 Linux .NET 资产")
        sys.exit(3)
    print("\t".join(picked))
    sys.exit(0)

with urllib.request.urlopen(api_base, timeout=30) as resp:
    releases = json.load(resp)

for release in releases:
    if release.get("draft") or release.get("prerelease"):
        continue
    picked = pick_asset(release)
    if picked is not None:
        print("\t".join(picked))
        sys.exit(0)

print("ERROR: 未找到可用 Godot Linux .NET 发行包")
sys.exit(4)
PY
}

install_godot_dotnet() {
    echo "-> 解析 Godot .NET 下载地址..."
    local info
    info="$(resolve_godot_asset)"
    if [[ "$info" == ERROR:* ]]; then
        echo "$info" >&2
        return 1
    fi

    local tag asset_name asset_url
    tag="$(printf '%s' "$info" | cut -f1)"
    asset_name="$(printf '%s' "$info" | cut -f2)"
    asset_url="$(printf '%s' "$info" | cut -f3)"

    echo "-> 将安装 Godot: tag=$tag, asset=$asset_name"
    local tmp_zip="/tmp/${asset_name}"
    curl -fL "$asset_url" -o "$tmp_zip"

    rm -rf "$GODOT_INSTALL_DIR"
    mkdir -p "$GODOT_INSTALL_DIR"
    unzip -q "$tmp_zip" -d "$GODOT_INSTALL_DIR"
    rm -f "$tmp_zip"

    local editor_bin
    editor_bin="$(find "$GODOT_INSTALL_DIR" -maxdepth 2 -type f \
        \( -name 'Godot_v*mono_linux*' -o -name 'Godot_v*dotnet_linux*' \) \
        ! -name '*console*' | head -n 1 || true)"

    if [[ -z "$editor_bin" ]]; then
        echo "未找到 Godot 图形编辑器可执行文件，请检查解压目录: $GODOT_INSTALL_DIR" >&2
        return 1
    fi

    chmod +x "$editor_bin"
    mkdir -p "$(dirname "$GODOT_BIN_LINK")"
    ln -sf "$editor_bin" "$GODOT_BIN_LINK"
    echo "-> Godot .NET 已安装: $editor_bin"
    echo "-> 命令链接已创建: $GODOT_BIN_LINK"
}

build_bridge() {
    echo "-> 编译 iBaye Godot bridge..."
    cmake -S "$ROOT_DIR" -B "$BUILD_DIR" -G Ninja \
        -DCMAKE_BUILD_TYPE=Release \
        -DBAYE_BUILD_GODOT_BRIDGE=ON
    cmake --build "$BUILD_DIR" -j"$(nproc)"

    local so="$BUILD_DIR/src/libibaye_godot_bridge.so"
    if [[ ! -f "$so" ]]; then
        echo "桥接库缺失: $so" >&2
        return 1
    fi
    cp "$so" "$ROOT_DIR/godot/libibaye_godot_bridge.so"
    echo "-> 已复制桥接库到: $ROOT_DIR/godot/libibaye_godot_bridge.so"
}

if [[ "$SKIP_DOTNET" -eq 0 ]]; then
    install_dotnet_sdk
fi

if [[ "$SKIP_GODOT" -eq 0 ]]; then
    install_godot_dotnet
fi

if [[ "$SKIP_BRIDGE" -eq 0 ]]; then
    build_bridge
fi

echo ""
echo "初始化完成。"
echo "下一步运行项目:"
echo "  $ROOT_DIR/scripts/run_godot_project.sh"

if [[ "$RUN_AFTER_SETUP" -eq 1 ]]; then
    echo "-> 直接启动 Godot 项目..."
    exec "$ROOT_DIR/scripts/run_godot_project.sh"
fi
