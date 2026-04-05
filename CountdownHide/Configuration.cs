using Dalamud.Configuration;
using System;

namespace CountdownHide;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    // When true the ScreenInfo_CountDown addon is hidden as soon as it appears.
    public bool HideCountdown { get; set; } = true;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
