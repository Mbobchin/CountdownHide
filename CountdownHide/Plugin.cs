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
    private const string WideTextAddonName  = "_WideText";
    private const string CommandName        = "/countdownhide";

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    public Configuration Configuration { get; init; }

    // True from PostSetup/PostShow of ScreenInfo_CountDown until its PreFinalize.
    // "Battle commencing" fires while this is FALSE (before countdown appears).
    // "Engage!" fires while this is TRUE (countdown is still running).
    private bool _countdownActive;

    // _WideText address stored when it appears before the countdown is confirmed.
    // Hidden once ScreenInfo_CountDown's event fires 1ms later.
    private nint _pendingWideTextAddr;

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

        // Countdown number overlay
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup,   CountdownAddonName, OnCountdownSetup);
        AddonLifecycle.RegisterListener(AddonEvent.PostShow,    CountdownAddonName, OnCountdownShow);
        // PreFinalize: clear pending so "Engage!" is never mistakenly hidden
        AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, CountdownAddonName, OnCountdownFinalize);

        // _WideText: "Battle commencing in X seconds!" — appears 1ms before ScreenInfo_CountDown
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, WideTextAddonName, OnWideTextShow);
        AddonLifecycle.RegisterListener(AddonEvent.PostShow,  WideTextAddonName, OnWideTextShow);

        // Catch-all for debug logging only
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, OnAnyAddonSetup);
        AddonLifecycle.RegisterListener(AddonEvent.PostShow,  OnAnyAddonShow);
    }

    public void Dispose()
    {
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup,   CountdownAddonName, OnCountdownSetup);
        AddonLifecycle.UnregisterListener(AddonEvent.PostShow,    CountdownAddonName, OnCountdownShow);
        AddonLifecycle.UnregisterListener(AddonEvent.PreFinalize, CountdownAddonName, OnCountdownFinalize);
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, WideTextAddonName, OnWideTextShow);
        AddonLifecycle.UnregisterListener(AddonEvent.PostShow,  WideTextAddonName, OnWideTextShow);
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
        if (Configuration.HideBattleCommencingText) HidePendingWideText();
    }

    private unsafe void OnCountdownShow(AddonEvent type, AddonArgs args)
    {
        _countdownActive = true;
        if (Configuration.HideCountdownOverlay) HideAddon(args);
        if (Configuration.HideBattleCommencingText) HidePendingWideText();
    }

    private void OnCountdownFinalize(AddonEvent type, AddonArgs args)
    {
        _countdownActive = false;
        _pendingWideTextAddr = nint.Zero;
    }

    // ── _WideText ("Battle commencing" text) ─────────────────────────────────

    private unsafe void OnWideTextShow(AddonEvent type, AddonArgs args)
    {
        if (_countdownActive)
        {
            // Countdown is running — this is the "Engage!" text (fires before PreFinalize).
            if (Configuration.HideEngageText)
                HideAddon(args);
        }
        else
        {
            // No countdown running yet — this may be "Battle commencing".
            // Store the address; hide it once ScreenInfo_CountDown confirms it.
            if (Configuration.HideBattleCommencingText)
                _pendingWideTextAddr = args.Addon.Address;
        }
    }

    private unsafe void HidePendingWideText()
    {
        if (_pendingWideTextAddr == nint.Zero) return;
        var addon = (AtkUnitBase*)_pendingWideTextAddr;
        _pendingWideTextAddr = nint.Zero;
        if (addon == null) return;
        addon->IsVisible = false;
        Log.Debug("[CountdownHide] Hid _WideText (Battle commencing)");
    }

    // ── Debug catch-all ───────────────────────────────────────────────────────

    private void OnAnyAddonSetup(AddonEvent type, AddonArgs args)
    {
        if (Configuration.DebugLogAddons)
            Log.Debug($"[CountdownHide] PostSetup: {args.AddonName}");
    }

    private void OnAnyAddonShow(AddonEvent type, AddonArgs args)
    {
        if (Configuration.DebugLogAddons)
            Log.Debug($"[CountdownHide] PostShow: {args.AddonName}");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static unsafe void HideAddon(AddonArgs args)
    {
        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null) return;
        addon->IsVisible = false;
        Log.Debug($"[CountdownHide] Hid {args.AddonName}");
    }

    // ── Command ───────────────────────────────────────────────────────────────

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
                Log.Info($"[CountdownHide] Countdown visuals now {(!both ? "hidden" : "visible")}.");
                break;
        }
    }
}
