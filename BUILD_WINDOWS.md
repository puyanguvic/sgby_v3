# iBaye Windows 构建与打包

本文档用于从源码构建 Windows 原生版 `baye.exe`，并优先打包为可直接运行的便携 `zip`。

## 1. 说明

- `README.md` 里的 `emconfigure cmake ...` 流程是 Emscripten/HTML5 目标（生成 `baye.js`）。
- Windows 原生目标在 `src/CMakeLists.txt` 的 `if (${CMAKE_SYSTEM_NAME} STREQUAL Windows)` 分支，产物为 `baye.exe`。

## 2. Ubuntu 一键出包（推荐）

这是在 Ubuntu 上更专业、可重复、可自动化（CI 友好）的方式：

1. 安装依赖

```bash
sudo apt update
sudo apt install -y cmake ninja-build mingw-w64 zip
```

2. 仓库根目录执行一键脚本

```bash
./build_windows_from_ubuntu.sh
```

默认产物：

- 便携包：`release/iBaye-windows-portable-1.0.0.zip`

脚本会自动完成：

- MinGW-w64 交叉编译 `baye.exe`
- 收集 `dat.lib`、`font.bin` 等运行资源
- 复制常见 MinGW 运行时 DLL

如果将来需要安装包，再执行：

```bash
sudo apt install -y nsis
./build_windows_from_ubuntu.sh --with-installer
```

会额外生成：

- 安装包：`release/iBaye-Setup-1.0.0.exe`

## 3. 本地预检后再发版（强烈建议）

为了避免反复消耗 GitHub Actions，建议每次发版前先本地预检：

```bash
./scripts/preflight_windows_release.sh --version=1.0.4
```

如果预检通过，再执行：

```bash
git tag -a v1.0.4 -m "Release v1.0.4"
git push origin v1.0.4
```

这样 CI 只负责“确认并发布”，不再承担试错。

## 4. 启用 pre-commit 本地门禁（建议）

在仓库根目录执行一次：

```bash
./scripts/install_git_hooks.sh
```

效果：

- 每次 `git commit` 前自动跑脚本语法检查
- 每次提交都自动执行一次本地构建预检：
  - `./scripts/preflight_windows_release.sh --version=precommit-local`
- 未通过则直接拒绝提交

如需临时跳过重检查（仅一次）：

```bash
RUN_FULL_PRECOMMIT_PRECHECK=0 git commit -m "your message"
```

## 5. Windows 本机构建环境（可选）

推荐使用 MSYS2 的 UCRT64 环境（Windows 11 上最稳定）：

1. 安装 MSYS2：<https://www.msys2.org/>
2. 打开 `MSYS2 UCRT64` 终端，执行：

```bash
pacman -Syu
pacman -S --needed git mingw-w64-ucrt-x86_64-toolchain mingw-w64-ucrt-x86_64-cmake mingw-w64-ucrt-x86_64-ninja
```

## 6. Windows 一键生成安装包（可选）

仓库根目录执行：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\build_windows_installer.ps1
```

或直接双击：

- `build_windows_installer.bat`

执行完成后安装包位于：

- `installer\iBaye-Setup.exe`

## 7. 手动编译 Windows 可执行文件（可选）

在 `MSYS2 UCRT64` 终端执行：

```bash
git clone --recurse-submodules <your_repo_url> iBaye
cd iBaye
cmake -S . -B build-win -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build build-win -j
```

产物通常在：

- `build-win/src/baye.exe`

## 8. 准备发布目录（dist）

`baye.exe` 启动时会从当前目录读取资源文件，至少需要：

- `baye.exe`
- `dat.lib`（仓库中通常是 `src/dat.lib.orig`，需要改名为 `dat.lib`）
- `font.bin`

在仓库根目录执行：

```bash
mkdir -p dist
cp build-win/src/baye.exe dist/
cp src/font.bin dist/
cp src/dat.lib.orig dist/dat.lib
```

如果需要，也可把自定义 `dat.lib` 放到 `dist/` 覆盖默认版本。

## 9. 复制 MinGW 运行时 DLL（如果缺失）

如果双击 `baye.exe` 报缺少 DLL（常见为 `libwinpthread-1.dll`、`libgcc_s_seh-1.dll`、`libstdc++-6.dll`），从以下目录复制到 `dist/`：

- `C:\msys64\ucrt64\bin`

示例（在 MSYS2 中）：

```bash
cp /ucrt64/bin/libwinpthread-1.dll dist/ || true
cp /ucrt64/bin/libgcc_s_seh-1.dll dist/ || true
cp /ucrt64/bin/libstdc++-6.dll dist/ || true
```

## 10. 使用 Inno Setup 打安装包

1. 安装 Inno Setup 6：<https://jrsoftware.org/isdl.php>
2. 仓库根目录已有 `installer.iss`，确保 `dist/` 已准备好。
3. 执行：

```powershell
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
```

输出目录：

- `installer\iBaye-Setup.exe`

## 11. 运行与权限建议

- 安装目录默认使用当前用户目录（`%LOCALAPPDATA%\Programs\iBaye`），避免存档写入权限问题。
- 存档文件会写在程序当前目录（如 `sango0.sav`、`sango1.sav` 等）。

## 12. CI 自动发布（GitHub Actions）

仓库已提供工作流：

- `.github/workflows/windows-release.yml`

触发方式：

1. 仅推送 tag 自动触发
- 推送形如 `v1.2.3` 的 tag
- 自动生成：
  - `release/iBaye-windows-portable-1.2.3.zip`
  - `release/SHA256SUMS.txt`
- 并自动创建 GitHub Release，附带以上文件
