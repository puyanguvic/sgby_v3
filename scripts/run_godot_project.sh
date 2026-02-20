#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT_DIR="${PROJECT_DIR:-$ROOT_DIR/godot}"
GODOT_BIN="${GODOT_BIN:-godot-dotnet}"
BRIDGE_SO="${BRIDGE_SO:-$PROJECT_DIR/libibaye_godot_bridge.so}"
FORCE_HEADLESS=0
FORCE_GUI=0
RUNTIME_ROOT="${RUNTIME_ROOT:-$ROOT_DIR/.godot_runtime}"
FORCE_LOCAL_XDG="${FORCE_LOCAL_XDG:-1}"
GODOT_LOG_FILE="${GODOT_LOG_FILE:-$RUNTIME_ROOT/godot.log}"

usage() {
    cat <<'EOF'
用法:
  ./scripts/run_godot_project.sh [--headless|--gui] [Godot 参数...]

说明:
  --headless  强制无图形模式（适合 CI/无桌面环境）
  --gui       强制图形模式

默认行为:
  若检测不到 DISPLAY/WAYLAND_DISPLAY，则自动追加 --headless。
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --headless)
            FORCE_HEADLESS=1
            shift
            ;;
        --gui)
            FORCE_GUI=1
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            break
            ;;
    esac
done

if [[ "$FORCE_HEADLESS" -eq 1 && "$FORCE_GUI" -eq 1 ]]; then
    echo "--headless 与 --gui 不能同时使用" >&2
    exit 1
fi

resolve_bin() {
    local candidate="$1"
    if [[ "$candidate" == */* ]]; then
        [[ -x "$candidate" ]] && { echo "$candidate"; return 0; }
        return 1
    fi
    command -v "$candidate" 2>/dev/null || return 1
}

BIN_PATH="$(resolve_bin "$GODOT_BIN" || true)"
if [[ -z "$BIN_PATH" ]]; then
    echo "找不到 Godot 可执行文件: $GODOT_BIN" >&2
    echo "先执行: ./scripts/setup_godot_dev_ubuntu.sh" >&2
    exit 1
fi

if [[ ! -f "$BRIDGE_SO" ]]; then
    echo "缺少桥接库: $BRIDGE_SO" >&2
    echo "先执行: ./scripts/setup_godot_dev_ubuntu.sh --skip-dotnet --skip-godot" >&2
    exit 1
fi

ensure_writable_xdg() {
    mkdir -p "$RUNTIME_ROOT/data" "$RUNTIME_ROOT/config" "$RUNTIME_ROOT/cache"

    if [[ "$FORCE_LOCAL_XDG" == "1" ]]; then
        export XDG_DATA_HOME="$RUNTIME_ROOT/data"
        export XDG_CONFIG_HOME="$RUNTIME_ROOT/config"
        export XDG_CACHE_HOME="$RUNTIME_ROOT/cache"
        return 0
    fi

    local default_data="${XDG_DATA_HOME:-$HOME/.local/share}"
    local default_config="${XDG_CONFIG_HOME:-$HOME/.config}"
    local default_cache="${XDG_CACHE_HOME:-$HOME/.cache}"

    if [[ ! -w "$default_data" ]]; then
        export XDG_DATA_HOME="$RUNTIME_ROOT/data"
    fi
    if [[ ! -w "$default_config" ]]; then
        export XDG_CONFIG_HOME="$RUNTIME_ROOT/config"
    fi
    if [[ ! -w "$default_cache" ]]; then
        export XDG_CACHE_HOME="$RUNTIME_ROOT/cache"
    fi
}

ensure_writable_xdg
export LD_LIBRARY_PATH="$PROJECT_DIR:${LD_LIBRARY_PATH:-}"

HEADLESS=0
if [[ "$FORCE_HEADLESS" -eq 1 ]]; then
    HEADLESS=1
elif [[ "$FORCE_GUI" -eq 1 ]]; then
    HEADLESS=0
elif [[ -z "${DISPLAY:-}" && -z "${WAYLAND_DISPLAY:-}" ]]; then
    HEADLESS=1
fi

if [[ "$HEADLESS" -eq 1 ]]; then
    echo "未检测到图形显示，使用 --headless 模式启动。"
    exec "$BIN_PATH" --headless --path "$PROJECT_DIR" --log-file "$GODOT_LOG_FILE" "$@"
fi

exec "$BIN_PATH" --path "$PROJECT_DIR" --log-file "$GODOT_LOG_FILE" "$@"
