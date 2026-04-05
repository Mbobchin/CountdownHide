using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System.Numerics;

namespace CountdownHide.Windows;

public class ConfigWindow : Window
{
    private readonly Configuration _config;

    public ConfigWindow(Configuration config) : base("CountdownHide Settings##CountdownHideConfig")
    {
        _config = config;
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(380, 210),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw()
    {
        ImGui.TextDisabled("Controls which parts of the /countdown visual are suppressed.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawToggle(
            "Hide countdown number overlay",
            "Hides the large on-screen number shown by /countdown.",
            ref _config.HideCountdownOverlay);

        DrawToggle(
            "Hide \"Battle commencing\" text",
            "Hides the \"Battle commencing in X seconds!\" notification\nthat appears when a countdown starts.",
            ref _config.HideBattleCommencingText);

        DrawToggle(
            "Hide \"Engage!\" text",
            "Hides the \"Engage!\" notification that appears\nwhen the countdown reaches zero.",
            ref _config.HideEngageText);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextDisabled("Developer");
        DrawToggle(
            "Log all addon events to /xllog",
            "Logs every addon PostSetup/PostShow event.\nUseful for identifying unknown addon names.",
            ref _config.DebugLogAddons);
    }

    private void DrawToggle(string label, string tooltip, ref bool value)
    {
        if (ImGui.Checkbox(label, ref value))
            _config.Save();
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(tooltip);
    }
}
