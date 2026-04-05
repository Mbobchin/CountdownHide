using Dalamud.Interface.Windowing;
using ImGuiNET;
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
            MinimumSize = new Vector2(360, 180),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw()
    {
        ImGui.TextDisabled("Controls which parts of the /countdown visual are suppressed.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ── Countdown number ───────────────────────────────────────────────
        var hideOverlay = _config.HideCountdownOverlay;
        if (ImGui.Checkbox("Hide countdown number overlay", ref hideOverlay))
        {
            _config.HideCountdownOverlay = hideOverlay;
            _config.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Hides the large on-screen number shown by /countdown.");

        // ── Battle commencing text ─────────────────────────────────────────
        var hideText = _config.HideBattleCommencingText;
        if (ImGui.Checkbox("Hide \"Battle commencing\" text", ref hideText))
        {
            _config.HideBattleCommencingText = hideText;
            _config.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Hides the \"Battle commencing in X seconds!\" notification\nthat appears alongside the countdown.");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // ── Debug ──────────────────────────────────────────────────────────
        ImGui.TextDisabled("Developer");
        var debug = _config.DebugLogAddons;
        if (ImGui.Checkbox("Log all addon events to /xllog", ref debug))
        {
            _config.DebugLogAddons = debug;
            _config.Save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Logs every addon PostSetup/PostShow event.\nTrigger a /countdown and check /xllog to identify addon names.");
    }
}
