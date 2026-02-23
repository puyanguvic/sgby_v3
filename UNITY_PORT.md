# iBaye Unity Port (Phase 1)

## Goal

Move shell/UI runtime from Godot to Unity while keeping the C engine unchanged.

## Migration Strategy

1. Keep engine core (`src/*.c`) as-is.
2. Keep current platform adapter (`src/platform/godot/*`) as the runtime backend for now.
3. Add a Unity-facing ABI layer (`src/platform/unity/*`) with stable symbol names.
4. Rebuild shell and UI behaviors in Unity scripts.

This avoids rewriting game logic and reduces migration risk.

## Delivered In This Phase

- New native header: `src/platform/unity/ibaye_unity_bridge.h`
- New native wrapper: `src/platform/unity/bridge_unity.c`
- New CMake option: `BAYE_BUILD_UNITY_BRIDGE`
- New build script: `scripts/build_unity_bridge.sh`
- Unity project scaffold under `unity/`
  - `Assets/Scripts/Core/IBayeNative.cs`
  - `Assets/Scripts/Core/IBayeHost.cs`
  - `Assets/Scripts/UI/IBayeViewport.cs`
  - `Assets/Scripts/UI/IBayeStatusLabel.cs`
  - `Assets/Scripts/UI/IBayeMainMenuPanel.cs`
  - `Assets/Scripts/UI/IBayeCityPanel.cs`
  - `Assets/Scripts/UI/IBayeBattleHud.cs`
  - `Assets/Scripts/UI/IBayeShellUI.cs`
  - `Assets/Scripts/UI/IBayeBootGuardPanel.cs`
  - `Assets/Editor/IBayeSceneBuilder.cs` (one-click scene generator)

## Build

```bash
./scripts/build_unity_bridge.sh
```

Native output (Linux):
- `unity/Assets/Plugins/Linux/x86_64/libibaye_unity_bridge.so`

Windows plugin build (cross-compile from Ubuntu):

```bash
./scripts/build_unity_windows_plugin.sh
```

Native output (Windows):
- `unity/Assets/Plugins/x86_64/ibaye_unity_bridge.dll`

Windows player package (requires Unity Editor CLI):

```bash
./scripts/build_unity_windows_player.sh --version=0.2.0 --unity-bin=/path/to/Unity
```

GitHub release publish:

```bash
./scripts/publish_unity_windows_release.sh --version=0.2.0 --unity-bin=/path/to/Unity
```

## Functional Baseline

Current baseline in Unity:

- Start engine in background thread.
- Poll and display RGBA frame buffer.
- Send keyboard and pointer input.
- Read runtime state via bridge API.
- Campaign start/continue menu.
- City list/detail display and city action key-sequence routing.
- Battle HUD (tempo + fight mode/weather/round alerts).
- BootGuard startup diagnostics + auto-fix/retry/ignore flow.
- One-click Unity scene generator (`IBaye > Create Default Shell Scene`).

## Next Phases

1. Build a committed Unity scene/prefab set (reduce manual wiring).
2. Replace Godot-specific deployment docs with Unity release pipeline.
3. Add smoke tests for plugin load + first frame + input loop.
4. Start migrating operation flow from key-sequence proxy to semantic commands.
