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
            MinimumSize = new Vector2(380, 220),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw()
    {
        ImGui.TextDisabled("Controls which parts of the /countdown visual are suppressed.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var hideOverlay = _config.HideCountdownOverlay;
        if (ImGui.Checkbox("Hide countdown number overlay", ref hideOverlay))
        {
            _config.HideCountdownOverlay = hideOverlay;
            _config.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Hides the large on-screen number shown by /countdown.");

        var hideBattle = _config.HideBattleCommencingText;
        if (ImGui.Checkbox("Hide \"Battle commencing\" text", ref hideBattle))
        {
            _config.HideBattleCommencingText = hideBattle;
            _config.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Hides the \"Battle commencing in X seconds!\" notification\nthat appears when a countdown starts.");

        var hideEngage = _config.HideEngageText;
        if (ImGui.Checkbox("Hide \"Engage!\" text", ref hideEngage))
        {
            _config.HideEngageText = hideEngage;
            _config.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Hides the \"Engage!\" notification that appears\nwhen the countdown reaches zero.");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextDisabled("Developer");

        var debug = _config.DebugLogAddons;
        if (ImGui.Checkbox("Log all addon events to /xllog", ref debug))
        {
            _config.DebugLogAddons = debug;
            _config.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Logs every addon PostSetup/PostShow event.\nUseful for identifying unknown addon names.");
    }
}
