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
- 安装脚本会优先尝试 `*_mono_export_templates.tpz` / `*_dotnet_export_templates.tpz`，并校验模板包含 C# loader。
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

若 Windows 端仍闪退：

- 运行导出包内的 `Run_With_Log.bat`
- 回传 `logs/ibaye_startup.log`
- 若仍报 C# loader 不可用，再运行 `Run_With_CoreHost_Trace.bat`
- 额外回传 `logs/dotnet_host_trace.log`

## 5. 常见闪退根因

1. 缺少 Export Templates（导出阶段就失败）。
2. 导出包缺少 `ibaye_godot_bridge.dll`。
3. 导出包缺少 `libwinpthread-1.dll`。
4. 导出包缺少 `dat.lib` 或字体资源。
5. Godot / Template / 项目版本不一致。
6. 日志出现 `No loader found for resource: res://*.cs`：
   - 原因：导出使用了非 .NET 模板（或模板损坏），`iBaye.exe` 不具备 C# 脚本加载器。
   - 处理：重新安装匹配版本模板并重导：
     `./scripts/install_godot_export_templates.sh --godot-bin=godot-dotnet`
     `./scripts/build_godot_windows_export.sh --version=<ver>`
   - 注意：大量 `Shader ... Loading cache ...` 与 `WASAPI ...` 日志是正常信息，不是崩溃根因。
   - 新包可先运行 `Diagnose_Runtime.bat`，确认 `data_iBayeGodotShell_windows_x86_64` 下关键文件齐全。
   - 若关键文件齐全仍报该错误，常见是系统/安全软件阻止 .NET host 加载。建议：
     1) 在解压目录执行 `Get-ChildItem -Recurse | Unblock-File`
     2) 关闭“就地运行压缩包”并重新完整解压到新目录
     3) 用 `Run_With_Log.bat` 重跑并附上 `logs/ibaye_startup.log`

## 5.1 架构改进建议（根治方向）

当前壳层使用 C#，优点是开发快；缺点是发布链路依赖 .NET host，受系统策略影响较大。  
如果目标是“发布后几乎不受目标机环境影响”，建议分阶段迁移为：

1. 启动层改为 GDScript（已完成），避免 C# loader 异常时直接崩溃。
2. 业务桥接从 `DllImport` 迁移到 GDExtension（C/C++），把原生能力暴露为 Godot Class。
3. UI 层逐步迁移为 GDScript（或 C++），使最终包不再依赖 .NET host。

这样可以从根本上消除 `No loader found for resource: *.cs` 这一类问题。

## 6. SmartScreen 与代码签名

未签名的 Windows 可执行文件会被 SmartScreen 标记为“未验证开发者”。

- 内部测试可手动选择“仍要运行”
- 正式发布要去除警告，需对 `iBaye.exe` 与关键 DLL 做 Authenticode 签名（OV/EV 证书）
## 7. 发布到 GitHub（本地导出后上传）

Windows 二进制不再在 GitHub Actions 上编译。推荐流程：

```bash
./scripts/publish_godot_windows_release.sh --version=1.0.12 --tag=v1.0.12 --push-tag
```

脚本会自动：

1. 本地预检并导出 Godot Windows 包
2. 生成 `release/godot-win/SHA256SUMS-<ver>.txt`
3. 创建/更新 `v<ver>` 对应的 GitHub Release 资产

并且仓库新增了 `Windows Release Smoke` 工作流：release 发布后会在 Windows runner 自动启动烟测，减少人工肉眼排查。
