#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PROJECT_DIR="${PROJECT_DIR:-$ROOT_DIR/godot}"
BUILD_DIR="${BUILD_DIR:-build-godot-win-export}"
OUTPUT_DIR="${OUTPUT_DIR:-release/godot-win}"
APP_VERSION="${APP_VERSION:-dev}"
PRESET_NAME="${PRESET_NAME:-Windows Desktop}"
GODOT_BIN="${GODOT_BIN:-godot-dotnet}"
FORCE_LOCAL_XDG="${FORCE_LOCAL_XDG:-1}"
RUNTIME_ROOT="${RUNTIME_ROOT:-$ROOT_DIR/.godot_runtime}"
DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-$ROOT_DIR/.dotnet-cli}"
NUGET_PACKAGES="${NUGET_PACKAGES:-$ROOT_DIR/.nuget/packages}"
AUTO_INSTALL_TEMPLATES=0

usage() {
    cat <<'EOF'
用途:
  在 Ubuntu 上一键导出 Godot Windows 包（含 bridge dll + 资源文件）。

用法:
  ./scripts/build_godot_windows_export.sh [选项]

选项:
  --version=<ver>         版本号（默认 dev）
  --preset=<name>         导出预设名（默认 Windows Desktop）
  --output-dir=<dir>      输出目录（默认 release/godot-win）
  --godot-bin=<bin>       Godot 可执行文件（默认 godot-dotnet）
  --install-templates     若缺模板则自动安装
  -h, --help              显示帮助

产物:
  release/godot-win/iBaye-godot-windows-<ver>.zip
EOF
}

for arg in "$@"; do
    case "$arg" in
        --version=*)
            APP_VERSION="${arg#*=}"
            ;;
        --preset=*)
            PRESET_NAME="${arg#*=}"
            ;;
        --output-dir=*)
            OUTPUT_DIR="${arg#*=}"
            ;;
        --godot-bin=*)
            GODOT_BIN="${arg#*=}"
            ;;
        --install-templates)
            AUTO_INSTALL_TEMPLATES=1
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

abspath() {
    local p="$1"
    if [[ "$p" = /* ]]; then
        printf '%s\n' "$p"
    else
        printf '%s\n' "$ROOT_DIR/$p"
    fi
}

PROJECT_DIR="$(abspath "$PROJECT_DIR")"
BUILD_DIR="$(abspath "$BUILD_DIR")"
OUTPUT_DIR="$(abspath "$OUTPUT_DIR")"
RUNTIME_ROOT="$(abspath "$RUNTIME_ROOT")"
DOTNET_CLI_HOME="$(abspath "$DOTNET_CLI_HOME")"
NUGET_PACKAGES="$(abspath "$NUGET_PACKAGES")"

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

ensure_runtime_env() {
    mkdir -p "$RUNTIME_ROOT/data" "$RUNTIME_ROOT/config" "$RUNTIME_ROOT/cache"
    if [[ "$FORCE_LOCAL_XDG" == "1" ]]; then
        export XDG_DATA_HOME="$RUNTIME_ROOT/data"
        export XDG_CONFIG_HOME="$RUNTIME_ROOT/config"
        export XDG_CACHE_HOME="$RUNTIME_ROOT/cache"
    fi
}

godot_template_version() {
    local raw
    raw="$("$GODOT_PATH" --version | head -n 1 | tr -d '\r\n')"
    if [[ "$raw" == *".official."* ]]; then
        printf '%s\n' "${raw%%.official*}"
        return 0
    fi
    printf '%s\n' "$raw"
}

ensure_export_templates() {
    local template_ver="$1"
    local template_dir="${XDG_DATA_HOME:-$HOME/.local/share}/godot/export_templates/$template_ver"
    local debug_tpl="$template_dir/windows_debug_x86_64.exe"
    local release_tpl="$template_dir/windows_release_x86_64.exe"

    if [[ -f "$debug_tpl" && -f "$release_tpl" ]]; then
        return 0
    fi

    if [[ "$AUTO_INSTALL_TEMPLATES" == "1" ]]; then
        "$ROOT_DIR/scripts/install_godot_export_templates.sh" \
            --godot-bin="$GODOT_PATH" \
            --template-version="$template_ver" \
            --templates-root="${XDG_DATA_HOME:-$HOME/.local/share}/godot/export_templates"
    fi

    if [[ ! -f "$debug_tpl" || ! -f "$release_tpl" ]]; then
        echo "缺少 Export Templates: $template_dir" >&2
        echo "先执行: ./scripts/install_godot_export_templates.sh --godot-bin=\"$GODOT_PATH\" --template-version=\"$template_ver\"" >&2
        exit 1
    fi
}

find_mingw_dll() {
    local dll="$1"
    local candidates=(
        "/usr/x86_64-w64-mingw32/lib"
        "/usr/lib/gcc/x86_64-w64-mingw32"
        "/usr/lib/gcc/x86_64-w64-mingw32/12-win32"
        "/usr/lib/gcc/x86_64-w64-mingw32/12-posix"
        "/usr/lib/gcc/x86_64-w64-mingw32/13-win32"
        "/usr/lib/gcc/x86_64-w64-mingw32/13-posix"
        "/usr/lib/gcc/x86_64-w64-mingw32/14-win32"
        "/usr/lib/gcc/x86_64-w64-mingw32/14-posix"
    )
    local dir
    for dir in "${candidates[@]}"; do
        if [[ -f "$dir/$dll" ]]; then
            printf '%s\n' "$dir/$dll"
            return 0
        fi
    done
    local found
    found="$(find /usr -type f -name "$dll" 2>/dev/null | head -n 1 || true)"
    if [[ -n "$found" ]]; then
        printf '%s\n' "$found"
        return 0
    fi
    return 1
}

copy_runtime_assets() {
    local target_dir="$1"
    local dat_src=""
    local font_src_dir=""

    for c in "$ROOT_DIR/dist-win/dat.lib" "$ROOT_DIR/src/dat.lib" "$ROOT_DIR/src/dat.lib.orig"; do
        if [[ -f "$c" ]]; then
            dat_src="$c"
            break
        fi
    done
    if [[ -z "$dat_src" ]]; then
        echo "缺少 dat.lib（尝试路径: dist-win/dat.lib, src/dat.lib, src/dat.lib.orig）" >&2
        exit 1
    fi

    for d in "$ROOT_DIR/dist-win" "$ROOT_DIR/src"; do
        if [[ -f "$d/font.bin" ]]; then
            font_src_dir="$d"
            break
        fi
    done
    if [[ -z "$font_src_dir" ]]; then
        echo "缺少 font.bin（尝试路径: dist-win/, src/）" >&2
        exit 1
    fi

    if [[ "$(basename "$dat_src")" == "dat.lib.orig" ]]; then
        cp "$dat_src" "$target_dir/dat.lib"
    else
        cp "$dat_src" "$target_dir/dat.lib"
    fi
    cp "$font_src_dir/font.bin" "$target_dir/font.bin"

    local required_fonts=(
        "font24.cn.1"
        "font24.cn.2"
        "font24.cn.3"
        "font24.cn.4"
        "font24.en.1"
        "font24.en.2"
    )
    local f
    for f in "${required_fonts[@]}"; do
        local found=""
        for d in "$ROOT_DIR/dist-win" "$ROOT_DIR/src"; do
            if [[ -f "$d/$f" ]]; then
                found="$d/$f"
                break
            fi
        done
        if [[ -z "$found" ]]; then
            echo "缺少字体资源: $f" >&2
            exit 1
        fi
        cp "$found" "$target_dir/$f"
    done
}

write_windows_runtime_helpers() {
    local target_dir="$1"

    cat >"$target_dir/Run_With_Log.bat" <<'EOF'
@echo off
setlocal
set "APP_DIR=%~dp0"
set "DOTNET_DIR=%APP_DIR%data_iBayeGodotShell_windows_x86_64"
set "LOG_DIR=%APP_DIR%logs"
set "CHECK_OK=1"
if not exist "%LOG_DIR%" mkdir "%LOG_DIR%"
set "LOG_FILE=%LOG_DIR%\ibaye_startup.log"

if not exist "%DOTNET_DIR%\" (
  echo [iBaye] ERROR: missing folder data_iBayeGodotShell_windows_x86_64
  set "CHECK_OK=0"
) else (
  for %%F in (GodotSharp.dll iBayeGodotShell.dll iBayeGodotShell.runtimeconfig.json hostfxr.dll hostpolicy.dll coreclr.dll) do (
    if not exist "%DOTNET_DIR%\%%F" (
      echo [iBaye] ERROR: missing runtime file data_iBayeGodotShell_windows_x86_64\%%F
      set "CHECK_OK=0"
    )
  )
)

if "%CHECK_OK%"=="0" (
  echo.
  echo [iBaye] startup aborted because required .NET runtime files are missing.
  echo [iBaye] run Diagnose_Runtime.bat and send the output to developer.
  echo [iBaye] tip: re-extract zip to a new folder, then run this script again.
  echo [iBaye] tip: if zip was downloaded from browser, unblock zip before extracting.
  pause
  exit /b 2
)

echo [iBaye] starting with verbose log...
echo [iBaye] log file: "%LOG_FILE%"
"%APP_DIR%iBaye.exe" --verbose --log-file "%LOG_FILE%"
set "EXIT_CODE=%ERRORLEVEL%"
echo.
echo [iBaye] exit code: %EXIT_CODE%
echo [iBaye] if startup failed, send "%LOG_FILE%" to developer.
pause
exit /b %EXIT_CODE%
EOF

    cat >"$target_dir/Diagnose_Runtime.bat" <<'EOF'
@echo off
setlocal
set "APP_DIR=%~dp0"
set "DOTNET_DIR=%APP_DIR%data_iBayeGodotShell_windows_x86_64"

echo [iBaye] Runtime diagnostics
echo [iBaye] app dir: %APP_DIR%
echo.

if not exist "%DOTNET_DIR%\" (
  echo [FAIL] missing folder: data_iBayeGodotShell_windows_x86_64
  goto :END
)

echo [ OK ] folder exists: data_iBayeGodotShell_windows_x86_64
for %%F in (GodotSharp.dll iBayeGodotShell.dll iBayeGodotShell.runtimeconfig.json hostfxr.dll hostpolicy.dll coreclr.dll) do (
  if exist "%DOTNET_DIR%\%%F" (
    echo [ OK ] %%F
  ) else (
    echo [FAIL] %%F
  )
)
echo.
echo [iBaye] If any [FAIL], re-download and re-extract the release zip.
echo [iBaye] If all [OK] but startup still fails, send logs\ibaye_startup.log to developer.

:END
pause
exit /b 0
EOF

    cat >"$target_dir/FIRST_RUN_README.txt" <<'EOF'
iBaye Windows First-Run Notes
=============================

1) SmartScreen warning (unverified publisher)
   If Windows shows "protected your PC", this is expected for unsigned builds.
   For internal test builds:
     More info -> Run anyway

2) Crash or immediate exit
   Run "Run_With_Log.bat" in this folder and share:
     logs\ibaye_startup.log
   If startup says missing runtime files, run:
     Diagnose_Runtime.bat

3) Public release requirement
   To remove SmartScreen warning for end users, binaries must be Authenticode-signed
   with a trusted OV/EV code signing certificate.
EOF
}

validate_dotnet_export() {
    local target_dir="$1"
    local data_dir=""

    data_dir="$(find "$target_dir" -maxdepth 1 -type d -name 'data_*_windows_x86_64' | head -n 1 || true)"
    if [[ -z "$data_dir" ]]; then
        echo "导出结果缺少 data_*_windows_x86_64 目录。" >&2
        echo "当前导出模板很可能不是 .NET 版本，运行时会报: No loader found for resource: *.cs" >&2
        echo "请确认使用 Godot .NET 编辑器 + 匹配的 Export Templates 后重试。" >&2
        exit 1
    fi

    if [[ ! -f "$data_dir/GodotSharp.dll" ]]; then
        echo "导出结果缺少 $data_dir/GodotSharp.dll。" >&2
        echo "这通常表示导出时使用了非 .NET 模板，包会在启动阶段崩溃。" >&2
        exit 1
    fi

    if ! find "$data_dir" -maxdepth 1 -type f -name '*.runtimeconfig.json' | grep -q .; then
        echo "导出结果缺少 *.runtimeconfig.json: $data_dir" >&2
        echo "C# 运行时配置未生成，无法正常加载 C# 脚本。" >&2
        exit 1
    fi
}

need_cmd cmake
need_cmd ninja
need_cmd dotnet
need_cmd x86_64-w64-mingw32-gcc
need_cmd x86_64-w64-mingw32-windres
need_cmd x86_64-w64-mingw32-objdump
need_cmd zip

GODOT_PATH="$(resolve_bin "$GODOT_BIN" || true)"
if [[ -z "$GODOT_PATH" ]]; then
    echo "找不到 Godot 可执行文件: $GODOT_BIN" >&2
    exit 1
fi

if [[ ! -f "$PROJECT_DIR/export_presets.cfg" ]]; then
    echo "缺少 $PROJECT_DIR/export_presets.cfg，无法使用 CLI 导出。" >&2
    exit 1
fi

ensure_runtime_env
mkdir -p "$DOTNET_CLI_HOME" "$NUGET_PACKAGES"
export DOTNET_CLI_HOME
export NUGET_PACKAGES

template_ver="$(godot_template_version)"
ensure_export_templates "$template_ver"

echo "== [1/5] 编译 Windows bridge dll =="
cmake -S "$ROOT_DIR" -B "$BUILD_DIR" -G Ninja \
    -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_SYSTEM_NAME=Windows \
    -DCMAKE_C_COMPILER=x86_64-w64-mingw32-gcc \
    -DCMAKE_RC_COMPILER=x86_64-w64-mingw32-windres \
    -DBAYE_BUILD_GODOT_BRIDGE=ON
cmake --build "$BUILD_DIR" -j"$(nproc)" --target ibaye_godot_bridge

bridge_dll="$BUILD_DIR/src/ibaye_godot_bridge.dll"
if [[ ! -f "$bridge_dll" ]]; then
    echo "bridge 构建完成但缺少产物: $bridge_dll" >&2
    exit 1
fi

echo "== [2/5] 构建 Godot C# 程序集 =="
dotnet build "$PROJECT_DIR/iBayeGodotShell.csproj" -c Release -v minimal

echo "== [3/5] 执行 Godot Windows 导出 =="
package_dir="$OUTPUT_DIR/iBaye-godot-win-$APP_VERSION"
zip_path="$OUTPUT_DIR/iBaye-godot-windows-$APP_VERSION.zip"
exe_path="$package_dir/iBaye.exe"

rm -rf "$package_dir"
mkdir -p "$package_dir"

"$GODOT_PATH" --headless --path "$PROJECT_DIR" --export-release "$PRESET_NAME" "$exe_path"

if [[ ! -f "$exe_path" ]]; then
    echo "导出失败: 未找到 $exe_path" >&2
    exit 1
fi

validate_dotnet_export "$package_dir"

echo "== [4/5] 收集原生库与运行资源 =="
cp "$bridge_dll" "$package_dir/ibaye_godot_bridge.dll"

mapfile -t dll_deps < <(
    x86_64-w64-mingw32-objdump -p "$bridge_dll" \
        | awk '/DLL Name:/ {print $3}' \
        | sort -u
)
for dll in "${dll_deps[@]}"; do
    case "${dll,,}" in
        kernel32.dll|user32.dll|gdi32.dll|msvcrt.dll|advapi32.dll|shell32.dll|ole32.dll|comdlg32.dll|ws2_32.dll)
            continue
            ;;
    esac
    if path="$(find_mingw_dll "$dll")"; then
        cp "$path" "$package_dir/$dll"
    else
        echo "缺少 bridge 依赖 DLL: $dll" >&2
        exit 1
    fi
done

copy_runtime_assets "$package_dir"
write_windows_runtime_helpers "$package_dir"

echo "== [5/5] 打包 ZIP =="
mkdir -p "$OUTPUT_DIR"
rm -f "$zip_path"
(
    cd "$package_dir"
    zip -9 -r "../$(basename "$zip_path")" .
)

echo ""
echo "完成。"
echo "导出目录: $package_dir"
echo "发布包: $zip_path"
