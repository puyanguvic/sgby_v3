using Godot;
using IBaye.GodotBridge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

public partial class BridgeHost : Node
{
    public struct RuntimeState
    {
        public int PlayerKing;
        public int CityCursor;
        public int CitySetX;
        public int CitySetY;
        public int FightMode;
        public int FightCity;
        public int FightOver;
        public int FightBout;
        public int FightWeather;
        public int FightActive;
    }

    [Export] public string DatPath = "";
    [Export] public string FontDir = "";
    [Export] public string SaveDir = "";
    [Export] public int ScreenWidth = 208;
    [Export] public int ScreenHeight = 128;
    [Export] public bool AutoStart = true;
    [Export] public int EngineReadyWaitMs = 3000;

    private byte[] _frameBuffer = Array.Empty<byte>();
    private uint _latestFrameId = 0;
    private uint _consumedFrameId = 0;
    private int _frameWidth = 0;
    private int _frameHeight = 0;
    private string _status = "未启动";
    private bool _startedByHost = false;
    private bool _nativeChecked = false;
    private bool _nativeAvailable = true;
    private string _nativeError = string.Empty;

    public string StatusText => _status;
    public bool IsRunning
    {
        get
        {
            if (!EnsureNativeAvailable())
            {
                return false;
            }
            try
            {
                return BridgeNative.ibaye_godot_is_running() != 0;
            }
            catch (Exception ex)
            {
                MarkNativeFailure("查询运行状态失败", ex);
                return false;
            }
        }
    }
    public bool IsStarted => _startedByHost || IsRunning;
    public int FrameWidth => _frameWidth;
    public int FrameHeight => _frameHeight;
    public int CurrentPeriod
    {
        get
        {
            if (!EnsureNativeAvailable())
            {
                return 0;
            }
            try
            {
                return BridgeNative.ibaye_godot_get_current_period();
            }
            catch (Exception ex)
            {
                MarkNativeFailure("读取剧本状态失败", ex);
                return 0;
            }
        }
    }
    public bool NativeAvailable => EnsureNativeAvailable();
    public string NativeIssue => _nativeError;

    public event Action<int> KeySent;
    public event Action<int, int, int> TouchSent;

    public override void _Ready()
    {
        AddToGroup("bridge_host");
        EnsureNativeAvailable();
        if (AutoStart)
        {
            StartEngine();
        }
    }

    public override void _Process(double delta)
    {
        PollFrame();
    }

    public bool StartEngine()
    {
        if (_startedByHost)
        {
            return true;
        }
        if (!Preflight(out string preflightReport))
        {
            _status = "启动前检查失败";
            GD.PushError(_status + ": " + preflightReport);
            return false;
        }
        if (!EnsureNativeAvailable())
        {
            return false;
        }

        string datPath = ResolvePath(DatPath);
        string fontDir = ResolvePath(FontDir);
        string saveDir = ResolvePath(SaveDir);

        if (!string.IsNullOrEmpty(datPath) || !string.IsNullOrEmpty(fontDir) || !string.IsNullOrEmpty(saveDir))
        {
            int rc;
            try
            {
                rc = BridgeNative.ibaye_godot_set_paths(
                    string.IsNullOrEmpty(datPath) ? null : datPath,
                    string.IsNullOrEmpty(fontDir) ? null : fontDir,
                    string.IsNullOrEmpty(saveDir) ? null : saveDir
                );
            }
            catch (Exception ex)
            {
                MarkNativeFailure("设置资源路径失败", ex);
                return false;
            }
            if (rc != 0)
            {
                _status = "设置资源路径失败: " + BridgeNative.LastError();
                GD.PushError(_status);
                return false;
            }
        }

        if (ScreenWidth > 0 && ScreenHeight > 0)
        {
            int rc;
            try
            {
                rc = BridgeNative.ibaye_godot_set_screen_size(ScreenWidth, ScreenHeight);
            }
            catch (Exception ex)
            {
                MarkNativeFailure("设置屏幕尺寸失败", ex);
                return false;
            }
            if (rc != 0)
            {
                _status = "设置屏幕尺寸失败: " + BridgeNative.LastError();
                GD.PushError(_status);
                return false;
            }
        }

        int startRc;
        try
        {
            startRc = BridgeNative.ibaye_godot_start();
        }
        catch (Exception ex)
        {
            MarkNativeFailure("启动失败", ex);
            return false;
        }
        if (startRc != 0)
        {
            _status = "启动失败: " + BridgeNative.LastError();
            GD.PushError(_status);
            return false;
        }

        _startedByHost = true;
        _status = "运行中";
        return true;
    }

    public bool LoadPeriod(int period)
    {
        if (!EnsureNativeAvailable())
        {
            return false;
        }

        if (!_startedByHost)
        {
            int queueRc;
            try
            {
                queueRc = BridgeNative.ibaye_godot_load_period(period);
            }
            catch (Exception ex)
            {
                MarkNativeFailure("加载剧本失败", ex);
                return false;
            }
            if (queueRc != 0)
            {
                _status = "加载剧本失败: " + BridgeNative.LastError();
                GD.PushError(_status);
                return false;
            }

            if (!StartEngine())
            {
                return false;
            }
        }

        if (!WaitForEngineReady(EngineReadyWaitMs))
        {
            _status = "引擎启动中，请稍后重试";
            GD.PushWarning(_status);
            return false;
        }

        int rc;
        try
        {
            rc = BridgeNative.ibaye_godot_load_period(period);
        }
        catch (Exception ex)
        {
            MarkNativeFailure("加载剧本失败", ex);
            return false;
        }
        if (rc != 0)
        {
            _status = "加载剧本失败: " + BridgeNative.LastError();
            GD.PushError(_status);
            return false;
        }

        _status = "运行中（剧本 " + period + "）";
        return true;
    }

    public void SendKey(int key)
    {
        if (!EnsureNativeAvailable())
        {
            return;
        }
        try
        {
            BridgeNative.ibaye_godot_send_key(key);
            KeySent?.Invoke(key);
        }
        catch (Exception ex)
        {
            MarkNativeFailure("发送按键失败", ex);
        }
    }

    public void SendTouch(int evt, int x, int y)
    {
        if (!EnsureNativeAvailable())
        {
            return;
        }
        try
        {
            BridgeNative.ibaye_godot_send_touch(evt, x, y);
            TouchSent?.Invoke(evt, x, y);
        }
        catch (Exception ex)
        {
            MarkNativeFailure("发送触控失败", ex);
        }
    }

    public bool TryBuildLatestImage(out Image image)
    {
        image = null;
        if (_latestFrameId == 0 || _latestFrameId == _consumedFrameId)
        {
            return false;
        }

        _consumedFrameId = _latestFrameId;
        image = Image.CreateFromData(_frameWidth, _frameHeight, false, Image.Format.Rgba8, _frameBuffer);
        return true;
    }

    public bool TryGetRuntimeState(out RuntimeState state)
    {
        state = default;
        if (!EnsureNativeAvailable())
        {
            return false;
        }
        if (!IsRunning)
        {
            return false;
        }

        int rc;
        try
        {
            rc = BridgeNative.ibaye_godot_get_runtime_state(
                out int playerKing,
                out int cityCursor,
                out int citySetX,
                out int citySetY,
                out int fightMode,
                out int fightCity,
                out int fightOver,
                out int fightBout,
                out int fightWeather,
                out int fightActive
            );
            state.PlayerKing = playerKing;
            state.CityCursor = cityCursor;
            state.CitySetX = citySetX;
            state.CitySetY = citySetY;
            state.FightMode = fightMode;
            state.FightCity = fightCity;
            state.FightOver = fightOver;
            state.FightBout = fightBout;
            state.FightWeather = fightWeather;
            state.FightActive = fightActive;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
        catch (Exception ex)
        {
            MarkNativeFailure("读取运行态失败", ex);
            return false;
        }

        return rc == 0;
    }

    private void PollFrame()
    {
        if (!EnsureNativeAvailable())
        {
            return;
        }

        int needed;
        try
        {
            needed = BridgeNative.ibaye_godot_get_frame_bytes();
        }
        catch (Exception ex)
        {
            MarkNativeFailure("读取帧缓冲大小失败", ex);
            return;
        }
        if (needed <= 0)
        {
            return;
        }

        if (_frameBuffer.Length < needed)
        {
            _frameBuffer = new byte[needed];
        }

        int copied;
        int w;
        int h;
        uint frameId;
        try
        {
            copied = BridgeNative.ibaye_godot_copy_frame(
                _frameBuffer,
                _frameBuffer.Length,
                out w,
                out h,
                out frameId
            );
        }
        catch (Exception ex)
        {
            MarkNativeFailure("拷贝帧缓冲失败", ex);
            return;
        }
        if (copied > 0)
        {
            _frameWidth = w;
            _frameHeight = h;
            _latestFrameId = frameId;
        }
    }

    private bool WaitForEngineReady(int timeoutMs)
    {
        if (IsRunning)
        {
            return true;
        }

        if (timeoutMs <= 0)
        {
            return false;
        }

        ulong start = Time.GetTicksMsec();
        while ((Time.GetTicksMsec() - start) < (ulong)timeoutMs)
        {
            if (IsRunning)
            {
                return true;
            }
            if (!NativeAvailable)
            {
                return false;
            }
            OS.DelayMsec(20);
        }
        return IsRunning;
    }

    public bool Preflight(out string report)
    {
        var issues = new List<string>();
        if (!EnsureNativeAvailable())
        {
            issues.Add(_nativeError);
        }

        string datPath = ResolvePath(DatPath);
        if (string.IsNullOrWhiteSpace(datPath) || !File.Exists(datPath))
        {
            issues.Add("dat.lib 路径无效，请在 BridgeHost.DatPath 指向 dat.lib");
        }

        string fontDir = ResolvePath(FontDir);
        string fontFile = string.IsNullOrWhiteSpace(fontDir) ? string.Empty : Path.Combine(fontDir, "font.bin");
        if (string.IsNullOrWhiteSpace(fontDir) || !Directory.Exists(fontDir) || !File.Exists(fontFile))
        {
            issues.Add("font.bin 路径无效，请在 BridgeHost.FontDir 指向包含 font.bin 的目录");
        }

        string saveDir = ResolvePath(SaveDir);
        if (string.IsNullOrWhiteSpace(saveDir))
        {
            issues.Add("存档目录未设置，请配置 BridgeHost.SaveDir");
        }
        else
        {
            try
            {
                Directory.CreateDirectory(saveDir);
            }
            catch (Exception ex)
            {
                issues.Add("存档目录不可写: " + ex.Message);
            }
        }

        report = issues.Count == 0 ? "启动检查通过" : string.Join("\n", issues);
        return issues.Count == 0;
    }

    public bool ApplyAutoFixes(out string report)
    {
        var logs = new List<string>();

        if (!HasValidDatPath())
        {
            string candidate = FindDatLibPath();
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                DatPath = candidate;
                logs.Add("已自动定位 dat.lib: " + candidate);
            }
        }

        if (!HasValidFontDir())
        {
            string candidate = FindFontDir();
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                FontDir = candidate;
                logs.Add("已自动定位 font.bin 目录: " + candidate);
            }
        }

        if (string.IsNullOrWhiteSpace(ResolvePath(SaveDir)))
        {
            SaveDir = ProjectSettings.GlobalizePath("user://save");
            logs.Add("已设置存档目录: " + SaveDir);
        }

        string saveDir = ResolvePath(SaveDir);
        if (!string.IsNullOrWhiteSpace(saveDir))
        {
            try
            {
                Directory.CreateDirectory(saveDir);
            }
            catch (Exception ex)
            {
                logs.Add("创建存档目录失败: " + ex.Message);
            }
        }

        bool ok = Preflight(out string preflightReport);
        if (logs.Count == 0)
        {
            report = preflightReport;
        }
        else
        {
            report = string.Join("\n", logs) + "\n" + preflightReport;
        }
        return ok;
    }

    private bool EnsureNativeAvailable()
    {
        if (_nativeChecked)
        {
            return _nativeAvailable;
        }

        _nativeChecked = true;
        try
        {
            BridgeNative.ibaye_godot_is_running();
            _nativeAvailable = true;
            return true;
        }
        catch (Exception ex)
        {
            MarkNativeFailure("加载 ibaye_godot_bridge 失败", ex);
            return false;
        }
    }

    private void MarkNativeFailure(string action, Exception ex)
    {
        _nativeAvailable = false;
        _nativeChecked = true;
        _nativeError = action + ": " + ex.GetType().Name + " - " + ex.Message;
        _status = _nativeError;
        GD.PushError(_nativeError);
    }

    private bool HasValidDatPath()
    {
        string datPath = ResolvePath(DatPath);
        return !string.IsNullOrWhiteSpace(datPath) && File.Exists(datPath);
    }

    private bool HasValidFontDir()
    {
        string fontDir = ResolvePath(FontDir);
        if (string.IsNullOrWhiteSpace(fontDir) || !Directory.Exists(fontDir))
        {
            return false;
        }
        return File.Exists(Path.Combine(fontDir, "font.bin"));
    }

    private static string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }
        if (path.StartsWith("res://", StringComparison.Ordinal) || path.StartsWith("user://", StringComparison.Ordinal))
        {
            return ProjectSettings.GlobalizePath(path);
        }
        return path;
    }

    private static string FindDatLibPath()
    {
        string[] candidates = new string[]
        {
            ProjectSettings.GlobalizePath("res://../dat.lib"),
            ProjectSettings.GlobalizePath("res://../dist-win/dat.lib"),
            ProjectSettings.GlobalizePath("res://dat.lib"),
            ProjectSettings.GlobalizePath("res://assets/dat.lib"),
            ProjectSettings.GlobalizePath("res://../data/dat.lib")
        };
        for (int i = 0; i < candidates.Length; i++)
        {
            if (File.Exists(candidates[i]))
            {
                return candidates[i];
            }
        }
        return string.Empty;
    }

    private static string FindFontDir()
    {
        string[] candidates = new string[]
        {
            ProjectSettings.GlobalizePath("res://.."),
            ProjectSettings.GlobalizePath("res://../dist-win"),
            ProjectSettings.GlobalizePath("res://../src"),
            ProjectSettings.GlobalizePath("res://"),
            ProjectSettings.GlobalizePath("res://assets"),
            ProjectSettings.GlobalizePath("res://../data")
        };
        for (int i = 0; i < candidates.Length; i++)
        {
            string f = Path.Combine(candidates[i], "font.bin");
            if (File.Exists(f))
            {
                return candidates[i];
            }
        }
        return string.Empty;
    }
}
