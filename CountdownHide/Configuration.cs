using Dalamud.Configuration;
using System;

namespace CountdownHide;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    // Hide the large on-screen countdown number (ScreenInfo_CountDown addon).
    public bool HideCountdownOverlay { get; set; } = true;

    // Hide the "Battle commencing in X seconds!" text that appears alongside it.
    public bool HideBattleCommencingText { get; set; } = true;

    // Log all addon PostSetup/PostShow events to /xllog for debugging.
    public bool DebugLogAddons { get; set; } = false;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
