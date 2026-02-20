#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BUILD_DIR="${BUILD_DIR:-/tmp/iBaye-build-godot}"
DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$ROOT_DIR/.dotnet-cli}"
NUGET_PACKAGES="${NUGET_PACKAGES:-$ROOT_DIR/.nuget/packages}"
GODOT_LOG="${GODOT_LOG:-$ROOT_DIR/.godot_runtime/verify-headless.log}"

usage() {
    cat <<'EOF'
用法:
  ./scripts/verify_godot_headless.sh

流程:
  1) 编译 Godot bridge
  2) 构建 C# 工程 (dotnet build)
  3) headless 启动 Godot 并快速退出
EOF
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
    usage
    exit 0
fi

need_cmd() {
    if ! command -v "$1" >/dev/null 2>&1; then
        echo "缺少命令: $1" >&2
        return 1
    fi
}

need_cmd cmake
need_cmd ninja
need_cmd dotnet

echo "== [1/3] 编译 bridge =="
cmake -S "$ROOT_DIR" -B "$BUILD_DIR" -G Ninja \
    -DCMAKE_BUILD_TYPE=Release \
    -DBAYE_BUILD_GODOT_BRIDGE=ON
cmake --build "$BUILD_DIR" -j"$(nproc)"
cp "$BUILD_DIR/src/libibaye_godot_bridge.so" "$ROOT_DIR/godot/libibaye_godot_bridge.so"

echo "== [2/3] 构建 C# =="
mkdir -p "$DOTNET_CLI_HOME" "$NUGET_PACKAGES"
(
    cd "$ROOT_DIR"
    DOTNET_CLI_HOME="$DOTNET_CLI_HOME" \
    NUGET_PACKAGES="$NUGET_PACKAGES" \
    dotnet build godot/iBayeGodotShell.csproj -c Debug -v minimal
)

echo "== [3/3] Headless 启动验证 =="
set +e
"$ROOT_DIR/scripts/run_godot_project.sh" --headless --quit --log-file "$GODOT_LOG" > /tmp/ibaye_headless_verify.out 2>&1
rc=$?
set -e
cat /tmp/ibaye_headless_verify.out

if rg -q "Cannot instantiate C# script|associated class could not be found" /tmp/ibaye_headless_verify.out; then
    echo "验证失败: C# 脚本仍未被正确加载。" >&2
    exit 2
fi

if rg -q "DllNotFoundException|undefined symbol|加载 ibaye_godot_bridge 失败|handle_crash|Program crashed|Segmentation fault" /tmp/ibaye_headless_verify.out; then
    echo "验证失败: Native bridge 加载或运行异常。" >&2
    exit 3
fi

if [[ $rc -ne 0 ]]; then
    echo "验证失败: Godot headless 返回非 0 ($rc)" >&2
    exit "$rc"
fi

echo "验证通过。"
