using Godot;
using IBaye.GodotBridge;

public partial class EngineViewport : TextureRect
{
    [Export] public NodePath HostPath;
    [Export] public bool CaptureInput = true;

    private BridgeHost _host;
    private ImageTexture _texture;

    public override void _Ready()
    {
        _host = GetNodeOrNull<BridgeHost>(HostPath);
        if (_host == null)
        {
            _host = GetTree().GetFirstNodeInGroup("bridge_host") as BridgeHost;
        }
        if (_host == null)
        {
            GD.PushError("EngineViewport: HostPath is not set or invalid.");
            return;
        }

        if (_host.ScreenWidth > 0 && _host.ScreenHeight > 0)
        {
            var bootImage = Image.CreateEmpty(_host.ScreenWidth, _host.ScreenHeight, false, Image.Format.Rgba8);
            _texture = ImageTexture.CreateFromImage(bootImage);
            Texture = _texture;
        }
    }

    public override void _Process(double delta)
    {
        if (_host == null)
        {
            return;
        }

        if (_host.TryBuildLatestImage(out Image image))
        {
            if (_texture == null)
            {
                _texture = ImageTexture.CreateFromImage(image);
                Texture = _texture;
            }
            else
            {
                _texture.Update(image);
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!CaptureInput || _host == null)
        {
            return;
        }

        if (@event is InputEventKey keyEvt && keyEvt.Pressed && !keyEvt.Echo)
        {
            if (TryMapKey(keyEvt.Keycode, out int mapped))
            {
                _host.SendKey(mapped);
                GetViewport().SetInputAsHandled();
            }
            return;
        }

        if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (TryMapPosToEngine(mb.Position, out int x, out int y))
            {
                _host.SendTouch(mb.Pressed ? BridgeNative.TouchDown : BridgeNative.TouchUp, x, y);
                GetViewport().SetInputAsHandled();
            }
            return;
        }

        if (@event is InputEventMouseMotion mm && Input.IsMouseButtonPressed(MouseButton.Left))
        {
            if (TryMapPosToEngine(mm.Position, out int x, out int y))
            {
                _host.SendTouch(BridgeNative.TouchMove, x, y);
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private bool TryMapKey(Key key, out int mapped)
    {
        mapped = 0;
        switch (key)
        {
            case Key.Enter:
            case Key.KpEnter:
                mapped = BridgeNative.KeyEnter;
                return true;
            case Key.Escape:
            case Key.Space:
            case Key.Backspace:
                mapped = BridgeNative.KeyExit;
                return true;
            case Key.Up:
                mapped = BridgeNative.KeyUp;
                return true;
            case Key.Down:
                mapped = BridgeNative.KeyDown;
                return true;
            case Key.Left:
                mapped = BridgeNative.KeyLeft;
                return true;
            case Key.Right:
                mapped = BridgeNative.KeyRight;
                return true;
            case Key.Pageup:
                mapped = BridgeNative.KeyPgUp;
                return true;
            case Key.Pagedown:
                mapped = BridgeNative.KeyPgDn;
                return true;
            default:
                return false;
        }
    }

    private bool TryMapPosToEngine(Vector2 globalPos, out int x, out int y)
    {
        x = 0;
        y = 0;

        if (_host.FrameWidth <= 0 || _host.FrameHeight <= 0)
        {
            return false;
        }

        var rect = GetGlobalRect();
        if (!rect.HasPoint(globalPos))
        {
            return false;
        }

        float relX = (globalPos.X - rect.Position.X) / rect.Size.X;
        float relY = (globalPos.Y - rect.Position.Y) / rect.Size.Y;

        relX = Mathf.Clamp(relX, 0.0f, 1.0f);
        relY = Mathf.Clamp(relY, 0.0f, 1.0f);

        x = Mathf.FloorToInt(relX * (_host.FrameWidth - 1));
        y = Mathf.FloorToInt(relY * (_host.FrameHeight - 1));
        return true;
    }
}
