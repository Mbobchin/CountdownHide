using CountdownHide.Windows;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CountdownHide;

public sealed class Plugin : IDalamudPlugin
{
    private const string CountdownAddonName = "ScreenInfo_CountDown";
    private const string CommandName = "/countdownhide";

    // Candidate addons for the "Battle commencing in X seconds!" text.
    // The one that actually matches will be logged to /xllog.
    private static readonly string[] BattleTextCandidates =
    [
        "_Notification",
        "_ScreenText",
        "SystemText",
        "_BattleTalk",
    ];

    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] internal static IGameGui GameGui { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
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

        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, CountdownAddonName, OnCountdownSetup);
        AddonLifecycle.RegisterListener(AddonEvent.PostShow, CountdownAddonName, OnCountdownShow);

        // Catch-all listener used for debug logging and to capture the
        // "Battle commencing" addon name (visible in /xllog).
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, OnAnyAddonSetup);
        AddonLifecycle.RegisterListener(AddonEvent.PostShow, OnAnyAddonShow);

        Framework.Update += OnFrameworkUpdate;
    }

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;

        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, CountdownAddonName, OnCountdownSetup);
        AddonLifecycle.UnregisterListener(AddonEvent.PostShow, CountdownAddonName, OnCountdownShow);
        AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, OnAnyAddonSetup);
        AddonLifecycle.UnregisterListener(AddonEvent.PostShow, OnAnyAddonShow);

        CommandManager.RemoveHandler(CommandName);

        PluginInterface.UiBuilder.Draw -= _windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= _configWindow.Toggle;
    }

    // ── Primary countdown number hider ───────────────────────────────────────

    private unsafe void OnCountdownSetup(AddonEvent type, AddonArgs args)
    {
        if (!Configuration.HideCountdownOverlay) return;
        HideAddon(args);
    }

    private unsafe void OnCountdownShow(AddonEvent type, AddonArgs args)
    {
        if (!Configuration.HideCountdownOverlay) return;
        HideAddon(args);
    }

    // ── Catch-all addon logger ────────────────────────────────────────────────

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

    // ── Per-frame: hide "Battle commencing" text while countdown is active ───

    private unsafe void OnFrameworkUpdate(IFramework framework)
    {
        if (!Configuration.HideBattleCommencingText) return;

        var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.CountDownSettingDialog);
        if (agent == null || !agent->IsAgentActive()) return;

        foreach (var name in BattleTextCandidates)
        {
            var atkPtr = GameGui.GetAddonByName(name);
            if (atkPtr.IsNull) continue;

            var addon = (AtkUnitBase*)atkPtr.Address;
            if (addon == null || !addon->IsVisible) continue;

            addon->IsVisible = false;
            Log.Info($"[CountdownHide] Hid battle text addon: {name}");
        }
    }

    private static unsafe void HideAddon(AddonArgs args)
    {
        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null) return;
        addon->IsVisible = false;
        Log.Debug($"[CountdownHide] Hid {args.AddonName}");
    }

    // ── Command handler ───────────────────────────────────────────────────────

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
                var state = !both ? "hidden" : "visible";
                Log.Info($"[CountdownHide] Countdown visuals are now {state}.");
                break;
        }
    }
}
