# Godot 第七阶段（运行态桥接 + 指令序列）

本目录现在是一个可直接打开的 Godot 4 C# 项目骨架，目标是：

- 保留 iBaye 原始 C 引擎逻辑
- 在 Godot 侧重做 UI/输入层
- 逐步把旧 UI 迁移为新的 2D 界面系统

## 目录结构（标准化）

- `project.godot`: Godot 项目配置（含 `autoload`）
- `scenes/main/Main.tscn`: 主场景（流程化菜单 + HUD + 启动自检 + 运行态）
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
./scripts/setup_godot_dev_ubuntu.sh --godot-tag=4.2.2-stable
```

### 手工方式

1. 在仓库根目录先构建桥接库（见 `GODOT_PORT.md`）。
2. 将库文件放到 Godot 项目可加载路径（Linux: `libibaye_godot_bridge.so`）。
3. 用 Godot 4 打开 `godot/project.godot`。
4. 打开并运行 `scenes/main/Main.tscn`。
5. 在 `BridgeHost` 节点里设置：
   - `DatPath`
   - `FontDir`
   - `SaveDir`

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
  - 键盘/鼠标输入映射到原引擎
  - 帧缓冲渲染
  - 城市面板：可切换历史时期并读取真实城市数据
  - 统一 UTF-8 中文文案（菜单、按钮、状态提示）
- 计划中：
  - 城市操作与旧引擎菜单状态机的精确映射（替代当前按键序列代理）
  - 战斗 HUD 与战斗内核数据直连（兵力/士气/阵型）
