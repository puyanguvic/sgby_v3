# iBaye

步步高电子词典经典游戏《三国霸业》移植版。

当前仓库采用双轨：

- 发布：Windows 便携包链路（Ubuntu 交叉编译）
- 开发调试：Godot-first（桥接层 + 原生无 UI debug host）
- Web 调试：Emscripten（浏览器端快速回归）

## Godot-first 调试入口（推荐）

先跑无 UI 调试端（最快定位引擎崩溃）：

```bash
./scripts/debug_godot_host.sh
```

说明：

- 默认只跑 `GamConInit + GamConRst`（启动烟测）
- 要跑完整引擎主循环：`MODE=engine ./scripts/debug_godot_host.sh`
- 可用环境变量覆盖资源路径：`DAT_PATH`、`FONT_DIR`、`DATA_DIR`

Godot 架构改造路线见：`ARCHITECTURE_GODOT_FIRST.md`

## Godot Windows 导出（标准流程）

1. 安装匹配版本的 Export Templates：

```bash
./scripts/install_godot_export_templates.sh
```

2. 一键导出 Godot Windows 包（含 bridge dll 与资源）：

```bash
./scripts/build_godot_windows_export.sh --version=0.1.0
```

3. 发布前预检：

```bash
./scripts/preflight_godot_windows_export.sh --version=0.1.0
```

产物：

- `release/godot-win/iBaye-godot-windows-0.1.0.zip`
- 包内含 `Run_With_Log.bat`，用于 Windows 闪退日志采集

签名说明（Windows SmartScreen）：

- 未签名构建会出现“未验证开发者/Windows 保护你的电脑”提示，这是系统正常行为。
- 对外发布要消除该提示，必须使用受信任代码签名证书（OV/EV）做 Authenticode 签名。

## Web 调试入口（优先定位崩溃）

先构建 Web 版本（依赖 emsdk / emcmake）：

```bash
./scripts/build_web_emscripten.sh
```

启动本地静态服务：

```bash
./scripts/serve_web_build.sh
```

打开 `http://127.0.0.1:8008`。

说明：

- Web 平台已内嵌 `dat.lib`、`font.bin`、`font24.cn.*`、`font24.en.*`，不依赖外部字体文件。
- 网页壳层 UI 使用浏览器字体（现代字体栈），和引擎内部像素字库是两层系统：
  - 壳层 UI 文案：可直接替换为任意现代字体。
  - 引擎帧缓冲文本：仍由旧字库渲染（后续如果彻底 UI/逻辑分离可逐步替换）。

## Ubuntu 环境配置（本地完整构建）

在 Ubuntu 执行：

```bash
sudo apt update
sudo apt install -y \
  git \
  cmake \
  ninja-build \
  mingw-w64 \
  zip \
  unzip \
  python3 \
  python3-yaml
```

可选：快速检查工具是否就绪

```bash
cmake --version
ninja --version
x86_64-w64-mingw32-gcc --version
x86_64-w64-mingw32-windres --version
```

## 本地完整构建（推荐）

1. 构建 Windows 便携包

```bash
./build_windows_from_ubuntu.sh --version=1.0.5
```

2. 本地预检（会验证 zip 产物和关键文件）

```bash
./scripts/preflight_windows_release.sh --version=1.0.5
```

产物（通过后）：

- `release/iBaye-windows-portable-1.0.5.zip`

## 本地提交门禁（建议开启）

```bash
./scripts/install_git_hooks.sh
```

开启后每次 `git commit` 会先做本地预检构建，失败会拒绝提交。

## 发布策略

- 本地先预检通过，再发布
- Windows 可执行包不再通过 GitHub Actions 构建（避免超大构建链路）
- 采用“本地导出 + 上传 GitHub Release 资产”

发布命令：

```bash
./scripts/publish_godot_windows_release.sh --version=1.0.12 --tag=v1.0.12 --push-tag
```

脚本会自动：

- 执行 `preflight_godot_windows_export.sh`
- 生成 `release/godot-win/iBaye-godot-windows-<ver>.zip`
- 生成 `SHA256SUMS-<ver>.txt`
- 创建或更新对应 tag 的 GitHub Release 资产

更多细节见：

- `BUILD_WINDOWS.md`
- `BUILD_GODOT_WINDOWS.md`
- `GODOT_PORT.md`（Godot 2D 适配阶段记录，当前到第七阶段）
- `godot/README.md`（含 Godot 导出与防闪退链路）
