# Godot Windows 导出与防闪退清单

目标：导出 `Godot 4.x + C# + ibaye_godot_bridge` 的 Windows 可运行包，并避免“启动即闪退”。

## 1. 必备条件

- Godot .NET 编辑器（示例：`4.6.1.stable.mono`）
- dotnet SDK 8
- MinGW 交叉编译工具链（用于构建 `ibaye_godot_bridge.dll`）
- 已配置 `godot/export_presets.cfg`（仓库已提供）

Ubuntu 依赖示例：

```bash
sudo apt update
sudo apt install -y cmake ninja-build mingw-w64 zip unzip curl dotnet-sdk-8.0
```

## 2. 安装 Export Templates（必须）

```bash
./scripts/install_godot_export_templates.sh
```

说明：

- 模板版本必须与 `godot-dotnet --version` 匹配。
- 缺模板时，CLI 导出会报：
  - `windows_debug_x86_64.exe not found`
  - `windows_release_x86_64.exe not found`

## 3. 一键导出

```bash
./scripts/build_godot_windows_export.sh --version=0.1.0
```

脚本会自动完成：

1. 交叉编译 `ibaye_godot_bridge.dll`
2. `dotnet build` C# 程序集
3. 调用 Godot CLI 执行 `Windows Desktop` 导出
4. 收集并打包运行时必需文件：
   - `ibaye_godot_bridge.dll`
   - `libwinpthread-1.dll`（bridge 依赖）
   - `dat.lib`
   - `font.bin`
   - `font24.cn.1..4`
   - `font24.en.1..2`

产物：

- `release/godot-win/iBaye-godot-windows-0.1.0.zip`

## 4. 发布前预检

```bash
./scripts/preflight_godot_windows_export.sh --version=0.1.0
```

预检会校验 zip 中关键文件是否齐全，避免“打包成功但运行缺文件”。

## 5. 常见闪退根因

1. 缺少 Export Templates（导出阶段就失败）。
2. 导出包缺少 `ibaye_godot_bridge.dll`。
3. 导出包缺少 `libwinpthread-1.dll`。
4. 导出包缺少 `dat.lib` 或字体资源。
5. Godot / Template / 项目版本不一致。
