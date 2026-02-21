using Godot;

public partial class BootGuardPanel : Control
{
    [Export] public NodePath HostPath;
    [Export] public NodePath MessageLabelPath;
    [Export] public NodePath FixButtonPath;
    [Export] public NodePath RetryButtonPath;
    [Export] public NodePath ContinueButtonPath;

    private BridgeHost _host;
    private RichTextLabel _message;
    private Button _fixButton;
    private Button _retryButton;
    private Button _continueButton;

    public override void _Ready()
    {
        _host = GetNodeOrNull<BridgeHost>(HostPath);
        if (_host == null)
        {
            _host = GetTree().GetFirstNodeInGroup("bridge_host") as BridgeHost;
        }

        _message = GetNodeOrNull<RichTextLabel>(MessageLabelPath);
        _fixButton = GetNodeOrNull<Button>(FixButtonPath);
        _retryButton = GetNodeOrNull<Button>(RetryButtonPath);
        _continueButton = GetNodeOrNull<Button>(ContinueButtonPath);

        if (_fixButton != null)
        {
            _fixButton.Pressed += OnFixPressed;
        }
        if (_retryButton != null)
        {
            _retryButton.Pressed += RefreshCheck;
        }
        if (_continueButton != null)
        {
            _continueButton.Pressed += OnContinuePressed;
        }

        RefreshCheck();
    }

    private void OnFixPressed()
    {
        if (_host == null)
        {
            SetMessage("[color=red]未找到 BridgeHost[/color]");
            return;
        }

        _host.ApplyAutoFixes(out string report);
        SetMessage(report);
        RefreshCheck();
    }

    private void OnContinuePressed()
    {
        Visible = false;
    }

    private void RefreshCheck()
    {
        if (_host == null)
        {
            Visible = true;
            SetMessage("[color=red]未找到 BridgeHost，请检查场景绑定。[/color]");
            return;
        }

        bool ok = _host.Preflight(out string report);
        if (ok)
        {
            Visible = false;
            return;
        }

        Visible = true;
        SetMessage("[b]启动自检未通过[/b]\n" + report);
    }

    private void SetMessage(string text)
    {
        if (_message != null)
        {
            _message.Text = text;
        }
    }
}
