using Godot;
using IBaye.GodotBridge;
using System.Collections.Generic;

public partial class BattleHud : Control
{
    [Export] public NodePath HostPath;
    [Export] public NodePath ModeLabelPath;
    [Export] public NodePath PeriodLabelPath;
    [Export] public NodePath LastCommandLabelPath;
    [Export] public NodePath TempoLabelPath;
    [Export] public NodePath TempoBarPath;
    [Export] public NodePath AlertLabelPath;

    private BridgeHost _host;
    private Label _modeLabel;
    private Label _periodLabel;
    private Label _lastCommandLabel;
    private Label _tempoLabel;
    private ProgressBar _tempoBar;
    private Label _alertLabel;
    private readonly Queue<long> _inputTicks = new Queue<long>();
    private string _lastCommand = "无";

    public override void _Ready()
    {
        _host = GetNodeOrNull<BridgeHost>(HostPath);
        if (_host == null)
        {
            _host = GetTree().GetFirstNodeInGroup("bridge_host") as BridgeHost;
        }

        _modeLabel = GetNodeOrNull<Label>(ModeLabelPath);
        _periodLabel = GetNodeOrNull<Label>(PeriodLabelPath);
        _lastCommandLabel = GetNodeOrNull<Label>(LastCommandLabelPath);
        _tempoLabel = GetNodeOrNull<Label>(TempoLabelPath);
        _tempoBar = GetNodeOrNull<ProgressBar>(TempoBarPath);
        _alertLabel = GetNodeOrNull<Label>(AlertLabelPath);

        if (_host != null)
        {
            _host.KeySent += OnKeySent;
            _host.TouchSent += OnTouchSent;
        }
        RenderHud();
    }

    public override void _Process(double delta)
    {
        TrimOldInput();
        RenderHud();
    }

    public override void _ExitTree()
    {
        if (_host != null)
        {
            _host.KeySent -= OnKeySent;
            _host.TouchSent -= OnTouchSent;
        }
    }

    private void OnKeySent(int key)
    {
        PushInput();
        _lastCommand = "按键: " + KeyName(key);
    }

    private void OnTouchSent(int evt, int x, int y)
    {
        PushInput();
        _lastCommand = "触控: " + evt + " (" + x + "," + y + ")";
    }

    private void PushInput()
    {
        _inputTicks.Enqueue((long)Time.GetTicksMsec());
        TrimOldInput();
    }

    private void TrimOldInput()
    {
        long now = (long)Time.GetTicksMsec();
        while (_inputTicks.Count > 0 && now - _inputTicks.Peek() > 8000)
        {
            _inputTicks.Dequeue();
        }
    }

    private void RenderHud()
    {
        int tempo = ComputeTempo();
        BridgeHost.RuntimeState rt = default;
        bool hasRuntime = false;
        if (_host != null)
        {
            hasRuntime = _host.TryGetRuntimeState(out rt);
        }
        bool fightActive = hasRuntime && rt.FightActive != 0;

        if (_tempoBar != null)
        {
            _tempoBar.Value = tempo;
        }
        if (_tempoLabel != null)
        {
            _tempoLabel.Text = "节奏 " + tempo + "%";
        }
        if (_lastCommandLabel != null)
        {
            _lastCommandLabel.Text = "最近指令: " + _lastCommand;
        }
        if (_periodLabel != null && _host != null)
        {
            if (hasRuntime)
            {
                _periodLabel.Text =
                    "剧本 " + _host.CurrentPeriod +
                    " / 势力 " + (rt.PlayerKing + 1) +
                    " / 光标城市 " + (rt.CityCursor + 1);
            }
            else
            {
                _periodLabel.Text = "剧本 " + _host.CurrentPeriod;
            }
        }
        if (_modeLabel != null)
        {
            if (fightActive)
            {
                _modeLabel.Text = "战斗-" + FightModeName(rt.FightMode);
            }
            else
            {
                _modeLabel.Text = tempo >= 70 ? "交战" : (tempo >= 30 ? "接敌" : "布阵");
            }
        }
        if (_alertLabel != null)
        {
            if (fightActive)
            {
                _alertLabel.Text =
                    "警报: 战斗中 / 回合 " + rt.FightBout +
                    " / 天气 " + WeatherName(rt.FightWeather) +
                    " / 目标城 " + (rt.FightCity + 1);
            }
            else
            {
                _alertLabel.Text = tempo >= 80 ? "警报: 战况激烈" : (tempo <= 15 ? "警报: 低活跃" : "警报: 正常");
            }
        }
    }

    private int ComputeTempo()
    {
        // 8 秒窗口，4 次/秒约等于 100%
        float actionsPerSecond = _inputTicks.Count / 8.0f;
        return Mathf.Clamp(Mathf.RoundToInt(actionsPerSecond / 4.0f * 100.0f), 0, 100);
    }

    private static string KeyName(int key)
    {
        switch (key)
        {
            case BridgeNative.KeyEnter:
                return "确认";
            case BridgeNative.KeyExit:
                return "返回";
            case BridgeNative.KeyUp:
                return "上";
            case BridgeNative.KeyDown:
                return "下";
            case BridgeNative.KeyLeft:
                return "左";
            case BridgeNative.KeyRight:
                return "右";
            case BridgeNative.KeyPgUp:
                return "上页";
            case BridgeNative.KeyPgDn:
                return "下页";
            default:
                return "0x" + key.ToString("X");
        }
    }

    private static string FightModeName(int mode)
    {
        switch (mode)
        {
            case 0:
                return "防御";
            case 1:
                return "进攻";
            case 2:
                return "自动";
            default:
                return "未知(" + mode + ")";
        }
    }

    private static string WeatherName(int weather)
    {
        switch (weather)
        {
            case 1:
                return "晴";
            case 2:
                return "阴";
            case 3:
                return "风";
            case 4:
                return "雨";
            case 5:
                return "雹";
            default:
                return "未知(" + weather + ")";
        }
    }
}
