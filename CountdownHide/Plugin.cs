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

    // Additional addon names that appear alongside the countdown.
    // Identified via /xllog while a countdown is running.
    private static readonly string[] ExtraAddonNames =
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

        // Catch-all: log every addon that appears so we can identify
        // what shows the "Battle commencing" text. Check /xllog while
        // triggering a /countdown to see the addon name.
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

    // ── Primary countdown hider ──────────────────────────────────────────────

    private unsafe void OnCountdownSetup(AddonEvent type, AddonArgs args)
    {
        if (!Configuration.HideCountdown) return;
        HideAddon(args);
    }

    private unsafe void OnCountdownShow(AddonEvent type, AddonArgs args)
    {
        if (!Configuration.HideCountdown) return;
        HideAddon(args);
    }

    // ── Catch-all logger: shows all addon names in /xllog ───────────────────

    private void OnAnyAddonSetup(AddonEvent type, AddonArgs args)
    {
        Log.Debug($"[CountdownHide] Addon PostSetup: {args.AddonName}");
    }

    private void OnAnyAddonShow(AddonEvent type, AddonArgs args)
    {
        Log.Debug($"[CountdownHide] Addon PostShow: {args.AddonName}");
    }

    // ── Per-frame: hide extra addons while countdown is active ───────────────

    private unsafe void OnFrameworkUpdate(IFramework framework)
    {
        if (!Configuration.HideCountdown) return;

        var agent = AgentModule.Instance()->GetAgentByInternalId(AgentId.CountDownSettingDialog);
        if (agent == null || !agent->IsAgentActive()) return;

        // Countdown is active — hide candidate addons and log which ones we find.
        foreach (var name in ExtraAddonNames)
        {
            var ptr = GameGui.GetAddonByName(name);
            if (ptr == nint.Zero) continue;

            var addon = (AtkUnitBase*)ptr;
            if (!addon->IsVisible) continue;

            addon->IsVisible = false;
            Log.Info($"[CountdownHide] Hid extra addon during countdown: {name}");
        }
    }

    private static unsafe void HideAddon(AddonArgs args)
    {
        var addon = (AtkUnitBase*)args.Addon.Address;
        if (addon == null) return;
        addon->IsVisible = false;
        Log.Debug($"[CountdownHide] Hid {args.AddonName}");
    }

    // ── Command handler ──────────────────────────────────────────────────────

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
                Configuration.HideCountdown = !Configuration.HideCountdown;
                Configuration.Save();
                var state = Configuration.HideCountdown ? "hidden" : "visible";
                Log.Info($"[CountdownHide] Countdown overlay is now {state}.");
                break;
        }
    }
}
