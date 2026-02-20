# iBaye

步步高电子词典经典游戏《三国霸业》移植版。

当前仓库聚焦于 Windows 发布链路（Ubuntu 交叉编译 + Windows 便携包发布）。

## 本地一键打 Windows 便携包（Ubuntu）

```bash
sudo apt update
sudo apt install -y cmake ninja-build mingw-w64 zip
./build_windows_from_ubuntu.sh
```

产物：

- `release/iBaye-windows-portable-<version>.zip`

## 发布策略

- 每次 `git commit` 前，pre-commit 会本地执行一次 Windows 预检构建
- 仅在推送 `v*` 标签（例如 `v1.0.4`）时触发 GitHub 发布

更多细节见：

- `BUILD_WINDOWS.md`
