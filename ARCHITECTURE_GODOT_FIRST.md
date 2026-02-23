# iBaye Godot-first 架构改造

## 目标

把“游戏核心”和“UI/平台壳”彻底分层，减少重复平台代码，提升调试效率与迭代速度。

## 分层

1. `Engine Core`（C）
- 游戏规则、数据结构、资源解析、战斗/城池逻辑
- 不直接依赖具体 UI 框架

2. `Runtime Adapter`（C）
- 负责输入事件、帧缓冲、文件/计时/线程等平台能力映射
- 当前主适配器：`platform/godot/*`
- 跨端前端桥接已抽象到 `platform/common/frontend_api.*`
  - `GamFrontendSendKey`
  - `GamFrontendSendTouch`
  - `GamFrontendCopyFrameRGBA`

3. `UI Shell`（Godot）
- 界面布局、交互、动画、HUD、面板流程
- 通过 `ibaye_godot_bridge` 与引擎通信

## 当前策略

1. Godot 作为唯一一等开发端
- 新功能优先走 bridge API + Godot UI，不再新增 Win32 专用 UI 逻辑

2. Windows 仅作为发布目标
- 继续保留发布脚本，但避免在旧窗口壳上做新功能开发

3. 无 UI debug host 作为日常回归入口
- 目标：先确认“核心可启动/可读资源/不崩溃”，再进 Godot UI 联调

## 已落地（第一步）

1. 新增 `ibaye_debug_host` 目标（`BAYE_BUILD_DEBUG_HOST=ON`）
2. 新增调试脚本：`scripts/debug_godot_host.sh`
3. README 增加 Godot-first 调试路径

## 已落地（第二步，进行中）

1. bridge 暴露统一生命周期状态：
- `ibaye_godot_get_engine_state()`
- `ibaye_godot_get_engine_state_name()`
2. 状态机覆盖 `IDLE/BOOTING/READY/RUNNING/EXITED/ERROR`
3. Godot `BridgeHost` 使用状态机驱动状态文案

## 后续改造顺序

1. 收敛平台实现
- 将 `fsys/timer/sem/sys/script` 的公共行为抽象到统一 runtime 接口
- 旧平台代码只保留最低维护
- Godot/Web 统一走 `frontend_api`，避免各端重复注入输入/拷贝帧逻辑

2. 收敛启动流程
- 引擎启动状态机（init/load/run/error）统一从 bridge 暴露
- 禁止 UI 层直接依赖旧平台内部约定

3. 扩展 bridge 业务 API
- 菜单、城池、战斗态、存档操作等能力按用例暴露
- Godot UI 只依赖 bridge，不穿透到 core 私有结构

4. 建立回归基线
- `debug_host` 启动烟测（无 UI）
- Godot 集成烟测（关键场景）

## 当前技术基线与 GDScript 迁移说明

当前基线：

- Godot 4.6 + C# bridge（`BridgeNative.cs` / `BridgeHost.cs`）+ C 动态库 `ibaye_godot_bridge`
- 该基线用于快速打通 native bridge 与跨平台导出链路

若目标改为“纯 GDScript 标准”：

1. 先补 GDExtension 封装层，把 C API 映射成 Godot 可直接调用的类
2. 再将 UI 与流程脚本从 C# 逐步迁移到 `.gd`
3. 保留同一套 bridge C 内核，避免重写引擎逻辑
