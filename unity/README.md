# iBaye Unity Shell (Phase 1)

This folder contains the initial Unity migration scaffold for the existing iBaye C engine.

Current migration scope:
- Keep the C engine unchanged.
- Reuse the native bridge ABI through `ibaye_unity_bridge`.
- Rebuild shell/UI in Unity.

## Folder Layout

- `Assets/Scripts/Core/IBayeNative.cs`: P/Invoke declarations.
- `Assets/Scripts/Core/IBayeHost.cs`: engine lifecycle + frame polling.
- `Assets/Scripts/UI/IBayeViewport.cs`: RawImage renderer + input mapping.
- `Assets/Scripts/UI/IBayeStatusLabel.cs`: optional status text binding.
- `Assets/Scripts/UI/IBayeMainMenuPanel.cs`: campaign start/continue menu.
- `Assets/Scripts/UI/IBayeCityPanel.cs`: period switch + city list/detail + action buttons.
- `Assets/Scripts/UI/IBayeBattleHud.cs`: combat HUD + tempo/alert display.
- `Assets/Scripts/UI/IBayeShellUI.cs`: top status + menu toggle + quick key buttons.
- `Assets/Scripts/UI/IBayeBootGuardPanel.cs`: startup diagnostics + auto-fix + retry/ignore.
- `Assets/Plugins/`: native plugin location.

## Build Native Plugin

From repository root:

```bash
./scripts/build_unity_bridge.sh
```

Linux output is copied to:
- `unity/Assets/Plugins/Linux/x86_64/libibaye_unity_bridge.so`

Build Windows plugin (cross-compile on Ubuntu):

```bash
./scripts/build_unity_windows_plugin.sh
```

Windows output is copied to:
- `unity/Assets/Plugins/x86_64/ibaye_unity_bridge.dll`
- plus required MinGW runtime DLLs (for example `libwinpthread-1.dll`)

Build Windows player package (requires Unity Editor CLI):

```bash
./scripts/build_unity_windows_player.sh --version=0.2.0 --unity-bin=/path/to/Unity
```

Publish a GitHub Release (build + upload):

```bash
./scripts/publish_unity_windows_release.sh --version=0.2.0 --unity-bin=/path/to/Unity
```

## Minimal Scene Wiring (Phase 1)

1. Open this folder as a Unity project.
2. Create a scene with a `Canvas` and a `RawImage`.
3. Create an empty GameObject named `IBayeHost` and attach `IBayeHost.cs`.
4. Attach `IBayeViewport.cs` to the `RawImage`, drag `IBayeHost` into `Host`.
5. Press Play.

Expected behavior:
- status becomes `booting/ready/running`.
- raw frame from engine appears in the `RawImage`.
- keyboard input (`Enter/Esc/Arrows/PageUp/PageDown`) reaches the engine.

## Extended Wiring (Phase 2)

1. Add a `MainMenuPanel` GameObject and attach `IBayeMainMenuPanel`.
2. Add `ShellRoot` GameObject and attach `IBayeShellUI`.
3. Add city panel widgets and attach `IBayeCityPanel`.
4. Add HUD labels/slider and attach `IBayeBattleHud`.
5. Add a BootGuard modal and attach `IBayeBootGuardPanel`.
6. Assign the same `IBayeHost` reference to all scripts above.

Recommended uGUI controls:
- Main menu: `Dropdown` (period), `Button` (start/continue), `Text` (status).
- City panel: `Dropdown` (city), `Text` (detail/log), `Button` (内政/军备/外交).
- Battle HUD: `Text` x5 + `Slider` x1.

## One-Click Scene Setup

You can auto-generate a runnable scene with bindings:

1. Open Unity project `unity/`.
2. In Unity menu click `IBaye > Create Default Shell Scene`.
3. Open generated scene: `Assets/Scenes/Main.unity`.

This builder creates and wires:
- `IBayeHost`
- `IBayeShellUI`
- `IBayeViewport`
- `IBayeMainMenuPanel`
- `IBayeCityPanel`
- `IBayeBattleHud`
- `IBayeBootGuardPanel`

## Notes

- Default paths assume this Unity project stays under repository root.
- `DatPath` defaults to `../dist-win/dat.lib`.
- `FontDir` defaults to `../dist-win`.
- `SaveDir` defaults to `Application.persistentDataPath`.
