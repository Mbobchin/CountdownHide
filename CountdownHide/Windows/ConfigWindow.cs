using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace CountdownHide.Windows;

public class ConfigWindow : Window
{
    private readonly Configuration _config;

    public ConfigWindow(Configuration config) : base("CountdownHide Settings##CountdownHideConfig")
    {
        _config = config;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new System.Numerics.Vector2(300, 100),
            MaximumSize = new System.Numerics.Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw()
    {
        var hide = _config.HideCountdown;
        if (ImGui.Checkbox("Hide countdown overlay", ref hide))
        {
            _config.HideCountdown = hide;
            _config.Save();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("When enabled, the large on-screen number shown by /countdown is hidden.\nThe countdown still fires and can be heard — only the visual is suppressed.");
    }
}
