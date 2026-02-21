# iBaye Godot 2D 适配（阶段记录）

当前仓库已提供一个可选的 Godot bridge 动态库目标：`ibaye_godot_bridge`。  
它的职责是：

- 在后台线程启动 iBaye 引擎
- 把按键/触摸输入转发给引擎
- 提供 RGBA 帧缓冲拷贝接口给 Godot 渲染层

## 1. 构建 bridge 库

```bash
cmake -S . -B build-godot-bridge -G Ninja \
  -DCMAKE_BUILD_TYPE=Release \
  -DBAYE_BUILD_GODOT_BRIDGE=ON
cmake --build build-godot-bridge -j
```

产物（Linux）通常在：

- `build-godot-bridge/src/libibaye_godot_bridge.so`

## 2. 运行时资源要求

引擎仍然依赖资源文件：

- `dat.lib`
- `font.bin`

建议在 Godot 启动时通过 `ibaye_godot_set_paths()` 显式设置：

- `dat_path`：`dat.lib` 的绝对路径
- `font_dir`：字体目录（应包含 `font.bin`）
- `data_dir`：存档目录

## 3. 核心 C API

头文件：`src/platform/godot/ibaye_godot_bridge.h`

- `ibaye_godot_set_paths(dat, font_dir, data_dir)`
- `ibaye_godot_set_screen_size(width, height)`
- `ibaye_godot_start()`
- `ibaye_godot_is_running()`
- `ibaye_godot_last_error()`
- `ibaye_godot_get_engine_state()`
- `ibaye_godot_get_engine_state_name()`
- `ibaye_godot_send_key(key)`
- `ibaye_godot_send_touch(event, x, y)`
- `ibaye_godot_get_frame_bytes()`
- `ibaye_godot_copy_frame(out, out_len, &w, &h, &frame_id)`

## 4. 输入映射建议

常用按键常量（头文件里已定义）：

- `IBAYE_KEY_ENTER`
- `IBAYE_KEY_EXIT`
- `IBAYE_KEY_UP`
- `IBAYE_KEY_DOWN`
- `IBAYE_KEY_LEFT`
- `IBAYE_KEY_RIGHT`
- `IBAYE_KEY_PGUP`
- `IBAYE_KEY_PGDN`

触摸事件：

- `IBAYE_TOUCH_DOWN`
- `IBAYE_TOUCH_UP`
- `IBAYE_TOUCH_MOVE`
- `IBAYE_TOUCH_CANCEL`

## 5. 当前阶段范围

已完成：

- 引擎核心 + 平台层的 Godot 可嵌入 bridge（C API）

未完成（下一阶段）：

- 完整 Godot 4 GDExtension 封装类
- Godot UI/输入配置面板
- 资源导入与存档路径的项目内自动配置

## 6. 第二阶段（已提供项目骨架）

仓库内新增了 Godot 项目骨架：

- `godot/project.godot`
- `godot/scenes/main/Main.tscn`
- `godot/scripts/core/BridgeHost.cs`
- `godot/scripts/ui/EngineViewport.cs`
- `godot/scripts/ui/ShellUI.cs`

定位：

- 这是“完全重置 UI”的起点，当前 UI 已与旧 Win32 窗口解耦。
- 旧引擎只负责逻辑与底层渲染帧，Godot 控制布局、交互和视觉风格。

## 7. 第三阶段（已接通城市业务数据）

bridge 增加了城市数据接口（`src/platform/godot/ibaye_godot_bridge.h`）：

- `ibaye_godot_get_current_period()`
- `ibaye_godot_load_period(period)`
- `ibaye_godot_get_city_count()`
- `ibaye_godot_get_city_name_bytes(city_index, out, len)`（GBK 字节）
- `ibaye_godot_get_city_stats(...)`

Godot 壳层新增：

- `godot/scripts/ui/CityPanel.cs`
- 主场景左侧改为“时期切换 + 城市列表 + 城市详情”

这使得 UI 重置不再只是皮肤，已经开始承接真实的游戏状态展示。

## 8. 第四阶段（原生菜单壳）

Godot 新增原生菜单面板：

- `godot/scripts/ui/MainMenuPanel.cs`
- `godot/scenes/main/Main.tscn` 中的 `MainMenu` 节点

能力：

- 启动前选择历史时期
- 由 Godot 菜单触发 `LoadPeriod + StartEngine`
- 运行中可通过顶部 `MENU` 按钮重新打开菜单

这一步把“进入游戏流程”从旧平台窗口逻辑迁移到了 Godot UI。

## 9. 第五阶段（流程化 UI + Unicode 文案）

本阶段继续重置 UI，重点不在“多一个面板”，而是把运行流程固定下来：

- 未开局时默认显示 `MainMenu`，隐藏主战场区（中间视口 + 左右功能区）
- 点击“开始”后触发 `LoadPeriod + StartEngine`，再显示主战场区
- 运行中可通过顶部“菜单”按钮再次打开 `MainMenu`
- 菜单“继续”仅在引擎已运行时可关闭菜单并回到主界面

并且统一了 Godot 壳层文案为 UTF-8 中文（按钮、状态、标题、提示），用于替代阶段性英文占位文本，减少乱码和编码混用风险。

## 10. 第六阶段（城市操作页 + 战斗 HUD + 启动自检）

本阶段完成你要求的 1/2/3 三项：

- 城市管理操作页：
  - 左侧城市面板新增“内政/军备/外交”操作按钮与操作日志
  - 操作会基于当前选中城市发送快捷指令到原引擎
- 战斗 HUD 重绘：
  - 中央战场视口叠加实时 HUD（布阵/接敌/交战、节奏条、最近指令、警报）
  - HUD 通过桥接层输入事件实时更新，节奏值基于最近 8 秒操作频率
- 启动自检与修复入口：
  - 新增 `BootGuard` 启动诊断面板（原生库、`dat.lib`、`font.bin`、存档目录）
  - 提供“自动修复 / 重新检测 / 忽略继续”
  - 忽略后也可通过顶部“菜单”重新进入诊断

## 11. 第七阶段（运行态桥接 + 指令序列）

本阶段继续完善“1/2/3”落地质量：

- bridge 新增 `ibaye_godot_get_runtime_state(...)`：
  - 输出玩家势力、地图光标城市、战斗模式/回合/天气、战斗活跃标记
  - 用于 HUD 和操作层决策，不再只靠按键频率推测状态
- 城市操作页新增“可配置按键序列”：
  - `DomesticKeySequence`
  - `MilitaryKeySequence`
  - `DiplomacyKeySequence`
  - 支持 `ENTER/EXIT/UP/DOWN/LEFT/RIGHT/PGUP/PGDN` 的逗号序列
- 战斗 HUD 优先显示真实战斗态：
  - 非战斗：沿用节奏态势（布阵/接敌/交战）
  - 战斗中：显示防御/进攻/自动、回合、天气、目标城市
