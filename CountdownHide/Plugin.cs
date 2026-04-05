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
    private const string CountdownAddonName = "ScreenInfo_CountDown";
    private const string CommandName = "/countdownhide";

    // Candidate addons for the "Battle commencing in X seconds!" text.
    // The winning name will appear in /xllog when debug logging is enabled.
    private static readonly string[] BattleTextCandidates =
    [
        "_WideText",        // "Battle commencing in X seconds!" — identified via /xllog
        "_Notification",
        "_ScreenText",
        "SystemText",
        "_BattleTalk",
    ];

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    public Configuration Configuration { get; init; }

    // True from the moment ScreenInfo_CountDown appears until it is finalized.
    private bool _countdownActive;

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

        // Primary countdown number addon
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup,   CountdownAddonName, OnCountdownSetup);
        AddonLifecycle.RegisterListener(AddonEvent.PostShow,    CountdownAddonName, OnCountdownShow);
        AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, CountdownAddonName, OnCountdownFinalize);

        // Catch-all: used for debug logging AND to intercept the battle text
        // addon the moment it appears while a countdown is running.
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, OnAnyAddonSetup);
        AddonLifecycle.RegisterListener(AddonEvent.PostShow,  OnAnyAddonShow);
    }

    public void Dispose()
    {
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup,   CountdownAddonName, OnCountdownSetup);
        AddonLifecycle.UnregisterListener(AddonEvent.PostShow,    CountdownAddonName, OnCountdownShow);
        AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, CountdownAddonName, OnCountdownFinalize);
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, OnAnyAddonSetup);
        AddonLifecycle.UnregisterListener(AddonEvent.PostShow,  OnAnyAddonShow);

        CommandManager.RemoveHandler(CommandName);
        PluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= _configWindow.Toggle;
    }

    // ── Countdown number ─────────────────────────────────────────────────────

    private unsafe void OnCountdownSetup(AddonEvent type, AddonArgs args)
    {
        _countdownActive = true;
        if (Configuration.HideCountdownOverlay) HideAddon(args);

        // Also sweep candidates immediately in case they already exist.
        if (Configuration.HideBattleCommencingText) HideCandidates();
    }

    private unsafe void OnCountdownShow(AddonEvent type, AddonArgs args)
    {
        _countdownActive = true;
        if (Configuration.HideCountdownOverlay) HideAddon(args);
        if (Configuration.HideBattleCommencingText) HideCandidates();
    }

    private void OnCountdownFinalize(AddonEvent type, AddonArgs args)
    {
        _countdownActive = false;
    }

    // ── Catch-all listener ───────────────────────────────────────────────────

    private void OnAnyAddonSetup(AddonEvent type, AddonArgs args)
    {
        if (Configuration.DebugLogAddons)
            Log.Debug($"[CountdownHide] PostSetup: {args.AddonName}");

        if (_countdownActive && Configuration.HideBattleCommencingText)
            TryHideBattleText(args);
    }

    private void OnAnyAddonShow(AddonEvent type, AddonArgs args)
    {
        if (Configuration.DebugLogAddons)
            Log.Debug($"[CountdownHide] PostShow: {args.AddonName}");

        if (_countdownActive && Configuration.HideBattleCommencingText)
            TryHideBattleText(args);
    }

    private unsafe void TryHideBattleText(AddonArgs args)
    {
        foreach (var name in BattleTextCandidates)
        {
            if (args.AddonName != name) continue;
            var addon = (AtkUnitBase*)args.Addon.Address;
            if (addon == null) return;
            addon->IsVisible = false;
            Log.Info($"[CountdownHide] Hid battle text addon: {name}");
            return;
        }
    }

    private unsafe void HideCandidates()
    {
        foreach (var name in BattleTextCandidates)
        {
            var ptr = GameGui.GetAddonByName(name);
            if (ptr.IsNull) continue;
            var addon = (AtkUnitBase*)ptr.Address;
            if (addon == null || !addon->IsVisible) continue;
            addon->IsVisible = false;
            Log.Info($"[CountdownHide] Hid existing battle text addon: {name}");
        }
    }

    private static unsafe void HideAddon(AddonArgs args)
    {
        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null) return;
        addon->IsVisible = false;
        Log.Debug($"[CountdownHide] Hid {args.AddonName}");
    }

    // ── Command ──────────────────────────────────────────────────────────────

    private void OnCommand(string command, string args)
    {
        switch (args.Trim().ToLowerInvariant())
        {
            case "config":
            case "settings":
                _configWindow.Toggle();
                break;

            default:
                var both = Configuration.HideCountdownOverlay && Configuration.HideBattleCommencingText;
                Configuration.HideCountdownOverlay = !both;
                Configuration.HideBattleCommencingText = !both;
                Configuration.Save();
                Log.Info($"[CountdownHide] Countdown visuals are now {(!both ? "hidden" : "visible")}.");
                break;
        }
    }
}
