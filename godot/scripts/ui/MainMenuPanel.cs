using Godot;
using System;

public partial class MainMenuPanel : Control
{
    [Export] public NodePath HostPath;
    [Export] public NodePath PeriodOptionPath;
    [Export] public NodePath StartButtonPath;
    [Export] public NodePath ContinueButtonPath;
    [Export] public NodePath StatusLabelPath;

    public event Action<int> CampaignStarted;
    public event Action MenuClosed;

    private BridgeHost _host;
    private OptionButton _periodOption;
    private Button _startButton;
    private Button _continueButton;
    private Label _status;

    public override void _Ready()
    {
        _host = GetNodeOrNull<BridgeHost>(HostPath);
        if (_host == null)
        {
            _host = GetTree().GetFirstNodeInGroup("bridge_host") as BridgeHost;
        }
        _periodOption = GetNodeOrNull<OptionButton>(PeriodOptionPath);
        _startButton = GetNodeOrNull<Button>(StartButtonPath);
        _continueButton = GetNodeOrNull<Button>(ContinueButtonPath);
        _status = GetNodeOrNull<Label>(StatusLabelPath);

        if (_periodOption != null && _periodOption.ItemCount == 0)
        {
            _periodOption.AddItem("董卓弄权 (1)");
            _periodOption.AddItem("曹操崛起 (2)");
            _periodOption.AddItem("赤壁之战 (3)");
            _periodOption.AddItem("三足鼎立 (4)");
        }

        if (_startButton != null)
        {
            _startButton.Pressed += OnStartPressed;
        }
        if (_continueButton != null)
        {
            _continueButton.Pressed += OnContinuePressed;
        }

        UpdateContinueButtonText();
        RefreshStatus();
    }

    public override void _Process(double delta)
    {
        RefreshStatus();
    }

    private void OnStartPressed()
    {
        if (_host == null)
        {
            SetStatus("未找到 BridgeHost");
            return;
        }

        int period = _periodOption == null ? 1 : _periodOption.GetSelectedId() + 1;
        if (!_host.LoadPeriod(period))
        {
            SetStatus("启动失败: " + _host.StatusText);
            return;
        }

        SetStatus("已进入剧本 " + period);
        Visible = false;
        CampaignStarted?.Invoke(period);
    }

    private void OnContinuePressed()
    {
        if (_host == null)
        {
            SetStatus("未找到 BridgeHost");
            return;
        }
        if (!_host.IsRunning)
        {
            SetStatus("引擎未启动，请先开始游戏");
            return;
        }

        Visible = false;
        MenuClosed?.Invoke();
    }

    public void OpenMenu()
    {
        Visible = true;
        UpdateContinueButtonText();
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        if (_status == null || _host == null)
        {
            return;
        }

        string s = _host.IsRunning ? "引擎运行中" : "引擎未启动";
        if (_host.CurrentPeriod > 0)
        {
            s += " / 剧本 " + _host.CurrentPeriod;
        }
        _status.Text = s;
        UpdateContinueButtonText();
    }

    private void SetStatus(string text)
    {
        if (_status != null)
        {
            _status.Text = text;
        }
    }

    private void UpdateContinueButtonText()
    {
        if (_continueButton == null)
        {
            return;
        }
        _continueButton.Text = _host != null && _host.IsRunning ? "继续" : "关闭";
    }
}
