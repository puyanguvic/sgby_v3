# iBaye

步步高电子词典经典游戏《三国霸业》移植版。

当前仓库聚焦于 Windows 发布链路（Ubuntu 交叉编译 + Windows 便携包发布）。

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
- 仅在推送 `v*` 标签（例如 `v1.0.5`）时触发 GitHub 发布

发布命令：

```bash
git tag -a v1.0.5 -m "Release v1.0.5"
git push origin v1.0.5
```

更多细节见：

- `BUILD_WINDOWS.md`
- `GODOT_PORT.md`（Godot 2D 适配阶段记录，当前到第七阶段）
- `godot/README.md`（含 Ubuntu 一键初始化脚本用法）
