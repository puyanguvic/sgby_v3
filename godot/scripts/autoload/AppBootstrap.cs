using Godot;

public partial class AppBootstrap : Node
{
    public bool IsBootstrapped { get; private set; } = false;

    public override void _Ready()
    {
        IsBootstrapped = true;
        GD.Print("[AppBootstrap] ready");
    }
}
