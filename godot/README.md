# Godot 第七阶段（运行态桥接 + 指令序列）

本目录现在是一个可直接打开的 Godot 4.x 项目骨架（当前桥接层为 C#），目标是：

- 保留 iBaye 原始 C 引擎逻辑
- 在 Godot 侧重做 UI/输入层
- 逐步把旧 UI 迁移为新的 2D 界面系统

## 目录结构（标准化）

- `project.godot`: Godot 项目配置（含 `autoload`）
- `scenes/main/Main.tscn`: 主场景（流程化菜单 + HUD + 启动自检 + 运行态）
- `scenes/ui/*.tscn`: 可复用 UI 子场景（主菜单/战斗 HUD/启动自检）
- `scripts/autoload/AppBootstrap.cs`: 全局启动入口（AutoLoad）
- `scripts/core/BridgeHost.cs`: 引擎桥接宿主
- `scripts/core/BridgeNative.cs`: `ibaye_godot_bridge` P/Invoke（含运行态读取）
- `scripts/ui/EngineViewport.cs`: 画面显示与输入映射
- `scripts/ui/ShellUI.cs`: 顶栏/侧栏/快捷按钮控制
- `scripts/ui/CityPanel.cs`: 时期切换 + 城市列表 + 城市详情
- `scripts/ui/MainMenuPanel.cs`: Godot 原生主菜单（先选时期再启动）
- `scripts/ui/BattleHud.cs`: 战斗 HUD（节奏、警报、最近指令）
- `scripts/ui/BootGuardPanel.cs`: 启动自检与自动修复面板
- `scripts/legacy/IBayeBridge.cs`: 第一阶段最小脚本（保留兼容）
- `scripts/README.md`: 脚本分层说明
- `addons/README.md`: Godot 插件目录占位

## 使用方式

### Ubuntu 一键初始化（推荐）

```bash
./scripts/setup_godot_dev_ubuntu.sh
./scripts/run_godot_project.sh
```

仅做 SSH 无界面验证：

```bash
./scripts/verify_godot_headless.sh
```

如需初始化后立即启动：

```bash
./scripts/setup_godot_dev_ubuntu.sh --run
```

可选：固定 Godot 版本（示例）：

```bash
./scripts/setup_godot_dev_ubuntu.sh --godot-tag=4.6.1-stable
```

### 手工方式

1. 在仓库根目录先构建桥接库（见 `GODOT_PORT.md`）。
2. 将库文件放到 Godot 项目可加载路径（Linux: `libibaye_godot_bridge.so`）。
3. 用 Godot 4 打开 `godot/project.godot`。
4. 打开并运行 `scenes/main/Main.tscn`。
5. 默认 `BridgeHost` 已预设开发路径：
   - `DatPath = res://../dist-win/dat.lib`
   - `FontDir = res://../dist-win`
   - `SaveDir = user://save`
6. 如目录结构有变化，可在 `BridgeHost` 节点里手工覆盖路径。

## Windows 导出（防闪退标准流程）

1. 安装 Export Templates（版本必须匹配当前 Godot）：

```bash
./scripts/install_godot_export_templates.sh
```

2. 一键导出 Windows 包（含 `ibaye_godot_bridge.dll`、字体和数据资源）：

```bash
./scripts/build_godot_windows_export.sh --version=0.1.0
```

3. 发布前本地预检（检查 zip 关键文件完整性）：

```bash
./scripts/preflight_godot_windows_export.sh --version=0.1.0
```

默认产物：

- `release/godot-win/iBaye-godot-windows-0.1.0.zip`

发布到 GitHub Release（本地导出后上传）：

```bash
./scripts/publish_godot_windows_release.sh --version=1.0.12 --tag=v1.0.12 --push-tag
```

注意：

- Godot CLI 导出依赖 `godot/export_presets.cfg`（仓库已提供）。
- 缺模板时会报错 `windows_debug_x86_64.exe / windows_release_x86_64.exe not found`。
- 导出包里必须包含 `ibaye_godot_bridge.dll` 和 `libwinpthread-1.dll`，否则 Windows 运行会高概率闪退。

## 关于 GDScript 标准化

当前项目为了调用 `ibaye_godot_bridge` C API，桥接层使用 C#（`BridgeNative.cs` + `BridgeHost.cs`）。  
如果要做到“纯 GDScript 标准”，需要先补一个 GDExtension 封装层，把 C API 变成 Godot Class/Singleton，再迁移 UI 脚本到 `.gd`。

## 当前状态

- 已实现：
  - Godot 原生主菜单（开始游戏/继续）
  - 启动前默认菜单态，启动后进入主战场
  - 新壳 UI（顶部状态、中央战场画布、右侧快捷键）
  - 城市管理操作页（内政/军备/外交）+ 操作日志
  - 战斗 HUD（战场态势、操作节奏、最近指令）
  - 运行态桥接：玩家势力、光标城市、战斗模式/回合/天气
  - 城市操作指令序列（可按场景配置快捷键组合）
  - 启动自检面板（原生库/资源/存档目录）+ 自动修复入口
  - 字体完整性校验（`font.bin + font24.cn.1..4 + font24.en.1..2`）
  - 键盘/鼠标输入映射到原引擎
  - 帧缓冲渲染
  - 城市面板：可切换历史时期并读取真实城市数据
  - 统一 UTF-8 中文文案（菜单、按钮、状态提示）
- 计划中：
  - 城市操作与旧引擎菜单状态机的精确映射（替代当前按键序列代理）
  - 战斗 HUD 与战斗内核数据直连（兵力/士气/阵型）
