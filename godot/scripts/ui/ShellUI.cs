using Godot;
using IBaye.GodotBridge;
using System;

public partial class ShellUI : Control
{
    [Export] public NodePath HostPath;
    [Export] public NodePath StatusLabelPath;
    [Export] public NodePath ClockLabelPath;
    [Export] public NodePath FpsLabelPath;
    [Export] public NodePath MainMenuPath;
    [Export] public NodePath GameplayRootPath;
    [Export] public NodePath BottomBarPath;
    [Export] public NodePath BootGuardPath;

    private BridgeHost _host;
    private Label _statusLabel;
    private Label _clockLabel;
    private Label _fpsLabel;
    private MainMenuPanel _mainMenu;
    private Control _gameplayRoot;
    private Control _bottomBar;
    private Control _bootGuard;
    private bool _campaignStarted;

    public override void _Ready()
    {
        _host = GetNodeOrNull<BridgeHost>(HostPath);
        if (_host == null)
        {
            _host = GetTree().GetFirstNodeInGroup("bridge_host") as BridgeHost;
        }
        _statusLabel = GetNodeOrNull<Label>(StatusLabelPath);
        _clockLabel = GetNodeOrNull<Label>(ClockLabelPath);
        _fpsLabel = GetNodeOrNull<Label>(FpsLabelPath);
        _mainMenu = GetNodeOrNull<MainMenuPanel>(MainMenuPath);
        _gameplayRoot = GetNodeOrNull<Control>(GameplayRootPath);
        _bottomBar = GetNodeOrNull<Control>(BottomBarPath);
        _bootGuard = GetNodeOrNull<Control>(BootGuardPath);

        if (_mainMenu != null)
        {
            _mainMenu.CampaignStarted += OnCampaignStarted;
            _mainMenu.MenuClosed += OnMenuClosed;
        }

        ApplyVisualStyle();
        _campaignStarted = _host != null && _host.IsRunning;
        SyncGameplayVisibility();
        if (!_campaignStarted)
        {
            _mainMenu?.OpenMenu();
        }
    }

    public override void _Process(double delta)
    {
        if (!_campaignStarted && _host != null && _host.IsRunning)
        {
            _campaignStarted = true;
            SyncGameplayVisibility();
        }

        if (_host != null && _statusLabel != null)
        {
            string uiState = _campaignStarted ? "战役中" : "启动菜单";
            _statusLabel.Text = _host.StatusText + " | " + uiState + " | P:" + _host.CurrentPeriod;
        }
        if (_clockLabel != null)
        {
            _clockLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        }
        if (_fpsLabel != null)
        {
            _fpsLabel.Text = "FPS " + Engine.GetFramesPerSecond();
        }
    }

    public override void _ExitTree()
    {
        if (_mainMenu != null)
        {
            _mainMenu.CampaignStarted -= OnCampaignStarted;
            _mainMenu.MenuClosed -= OnMenuClosed;
        }
    }

    public void OnEnterPressed()
    {
        _host?.SendKey(BridgeNative.KeyEnter);
    }

    public void OnExitPressed()
    {
        _host?.SendKey(BridgeNative.KeyExit);
    }

    public void OnUpPressed()
    {
        _host?.SendKey(BridgeNative.KeyUp);
    }

    public void OnDownPressed()
    {
        _host?.SendKey(BridgeNative.KeyDown);
    }

    public void OnLeftPressed()
    {
        _host?.SendKey(BridgeNative.KeyLeft);
    }

    public void OnRightPressed()
    {
        _host?.SendKey(BridgeNative.KeyRight);
    }

    public void OnPgUpPressed()
    {
        _host?.SendKey(BridgeNative.KeyPgUp);
    }

    public void OnPgDnPressed()
    {
        _host?.SendKey(BridgeNative.KeyPgDn);
    }

    public void OnMenuPressed()
    {
        if (_host != null && !_host.Preflight(out _))
        {
            if (_bootGuard != null)
            {
                _bootGuard.Visible = true;
                return;
            }
        }
        _mainMenu?.OpenMenu();
    }

    private void ApplyVisualStyle()
    {
        var panelStyle = new StyleBoxFlat
        {
            BgColor = new Color("1a2029"),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color("3a4b5f"),
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10
        };

        var buttonNormal = new StyleBoxFlat
        {
            BgColor = new Color("243241"),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color("4f6a82"),
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8
        };
        var buttonHover = (StyleBoxFlat)buttonNormal.Duplicate();
        buttonHover.BgColor = new Color("2c3f52");
        var buttonPressed = (StyleBoxFlat)buttonNormal.Duplicate();
        buttonPressed.BgColor = new Color("1b8f7a");

        var theme = new Theme();
        theme.SetStylebox("panel", "PanelContainer", panelStyle);
        theme.SetStylebox("normal", "Button", buttonNormal);
        theme.SetStylebox("hover", "Button", buttonHover);
        theme.SetStylebox("pressed", "Button", buttonPressed);
        theme.SetColor("font_color", "Label", new Color("d8ecff"));
        theme.SetColor("font_color", "Button", new Color("e8f6ff"));

        Theme = theme;
    }

    private void OnCampaignStarted(int period)
    {
        _campaignStarted = true;
        SyncGameplayVisibility();
    }

    private void OnMenuClosed()
    {
        SyncGameplayVisibility();
    }

    private void SyncGameplayVisibility()
    {
        if (_gameplayRoot != null)
        {
            _gameplayRoot.Visible = _campaignStarted;
        }
        if (_bottomBar != null)
        {
            _bottomBar.Visible = _campaignStarted;
        }
    }
}
