using Godot;
using System;
using System.Runtime.InteropServices;

public partial class IBayeBridge : Node
{
    [Export] public TextureRect Target;
    [Export] public string DatPath = "";
    [Export] public string FontDir = "";
    [Export] public string SaveDir = "";
    [Export] public int Width = 208;
    [Export] public int Height = 128;

    private ImageTexture _texture;
    private byte[] _frameBytes = Array.Empty<byte>();
    private uint _lastFrameId = 0;

    [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ibaye_godot_set_paths(string datPath, string fontDir, string dataDir);

    [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ibaye_godot_set_screen_size(int width, int height);

    [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ibaye_godot_start();

    [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr ibaye_godot_last_error();

    [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ibaye_godot_get_frame_bytes();

    [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
    private static extern int ibaye_godot_copy_frame(
        byte[] outRgba,
        int outLen,
        out int outWidth,
        out int outHeight,
        out uint outFrameId
    );

    [DllImport("ibaye_godot_bridge", CallingConvention = CallingConvention.Cdecl)]
    private static extern void ibaye_godot_send_key(int key);

    public override void _Ready()
    {
        if (Target == null)
        {
            GD.PrintErr("IBayeBridge: Target TextureRect is not assigned.");
            return;
        }

        if (!string.IsNullOrEmpty(DatPath) || !string.IsNullOrEmpty(FontDir) || !string.IsNullOrEmpty(SaveDir))
        {
            if (ibaye_godot_set_paths(
                    string.IsNullOrEmpty(DatPath) ? null : DatPath,
                    string.IsNullOrEmpty(FontDir) ? null : FontDir,
                    string.IsNullOrEmpty(SaveDir) ? null : SaveDir
                ) != 0)
            {
                GD.PrintErr("IBayeBridge set_paths failed: " + PtrToString(ibaye_godot_last_error()));
            }
        }

        if (Width > 0 && Height > 0)
        {
            if (ibaye_godot_set_screen_size(Width, Height) != 0)
            {
                GD.PrintErr("IBayeBridge set_screen_size failed: " + PtrToString(ibaye_godot_last_error()));
            }
        }

        if (ibaye_godot_start() != 0)
        {
            GD.PrintErr("IBayeBridge start failed: " + PtrToString(ibaye_godot_last_error()));
        }

        _texture = ImageTexture.CreateFromImage(Image.CreateEmpty(Width, Height, false, Image.Format.Rgba8));
        Target.Texture = _texture;
    }

    public override void _Process(double delta)
    {
        HandleKeyboard();
        RenderFrame();
    }

    private void HandleKeyboard()
    {
        if (Input.IsActionJustPressed("ui_accept")) ibaye_godot_send_key(0x27); // ENTER
        if (Input.IsActionJustPressed("ui_cancel")) ibaye_godot_send_key(0x28); // EXIT
        if (Input.IsActionJustPressed("ui_up")) ibaye_godot_send_key(0x22);
        if (Input.IsActionJustPressed("ui_down")) ibaye_godot_send_key(0x23);
        if (Input.IsActionJustPressed("ui_left")) ibaye_godot_send_key(0x24);
        if (Input.IsActionJustPressed("ui_right")) ibaye_godot_send_key(0x25);
    }

    private void RenderFrame()
    {
        int needed = ibaye_godot_get_frame_bytes();
        if (needed <= 0)
        {
            return;
        }

        if (_frameBytes.Length < needed)
        {
            _frameBytes = new byte[needed];
        }

        int copied = ibaye_godot_copy_frame(_frameBytes, _frameBytes.Length, out int w, out int h, out uint frameId);
        if (copied <= 0 || frameId == _lastFrameId)
        {
            return;
        }
        _lastFrameId = frameId;

        var image = Image.CreateFromData(w, h, false, Image.Format.Rgba8, _frameBytes);
        _texture.Update(image);
    }

    private static string PtrToString(IntPtr p)
    {
        return p == IntPtr.Zero ? "" : Marshal.PtrToStringAnsi(p) ?? "";
    }
}
