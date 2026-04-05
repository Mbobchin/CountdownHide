using CountdownHide.Windows;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CountdownHide;

public sealed class Plugin : IDalamudPlugin
{
    // The AtkUnitBase name used by the game's countdown overlay.
    private const string CountdownAddonName = "ScreenInfo_CountDown";
    private const string CommandName = "/countdownhide";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    public Configuration Configuration { get; init; }

    private readonly WindowSystem _windowSystem = new("CountdownHide");
    private readonly ConfigWindow _configWindow;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        _configWindow = new ConfigWindow(Configuration);
        _windowSystem.AddWindow(_configWindow);

        PluginInterface.UiBuilder.Draw += _windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += _configWindow.Toggle;

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the countdown overlay on/off, or open settings with 'config'.",
        });

        // Hook the addon immediately after the game creates it so we can hide it
        // before the first frame renders.
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, CountdownAddonName, OnCountdownSetup);

        // Also catch every subsequent show so that if the player opens and closes
        // the countdown dialog in settings, or if the addon is shown again mid-session,
        // we suppress it every time.
        AddonLifecycle.RegisterListener(AddonEvent.PostShow, CountdownAddonName, OnCountdownShow);
    }

    public void Dispose()
    {
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, CountdownAddonName, OnCountdownSetup);
        AddonLifecycle.UnregisterListener(AddonEvent.PostShow, CountdownAddonName, OnCountdownShow);

        CommandManager.RemoveHandler(CommandName);

        PluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= _configWindow.Toggle;
    }

    // Called once when the game constructs the ScreenInfo_CountDown addon.
    private unsafe void OnCountdownSetup(AddonEvent type, AddonArgs args)
    {
        if (!Configuration.HideCountdown) return;
        HideAddon(args);
    }

    // Called every time the addon transitions from hidden → visible.
    private unsafe void OnCountdownShow(AddonEvent type, AddonArgs args)
    {
        if (!Configuration.HideCountdown) return;
        HideAddon(args);
    }

    private static unsafe void HideAddon(AddonArgs args)
    {
        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null) return;
        addon->IsVisible = false;
        Log.Debug($"[CountdownHide] Hid {args.AddonName}");
    }

    private void OnCommand(string command, string args)
    {
        var trimmed = args.Trim().ToLowerInvariant();
        switch (trimmed)
        {
            case "config":
            case "settings":
                _configWindow.Toggle();
                break;

            default:
                // No argument → toggle the feature and report the new state.
                Configuration.HideCountdown = !Configuration.HideCountdown;
                Configuration.Save();

                var state = Configuration.HideCountdown ? "hidden" : "visible";
                Log.Info($"[CountdownHide] Countdown overlay is now {state}.");
                break;
        }
    }
}
