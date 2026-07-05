using CS2_Admin.Config;
using CS2_Admin.Database;
using CS2_Admin.Services;
using CS2_Admin.Utils;
using System.Net;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;

namespace CS2_Admin.Commands;

/// <summary>
/// Ban ailesi komutlarının (ban/ipban/addban/unban/lastban) ortak bağımlılıkları ve
/// offline ban yardımcıları. Bu üyeler önceden her komut dosyasında kopyaydı; kopyalardan
/// birindeki await eksikliği "zaten banlı" bug'ını dört ayrı yerde üretmişti.
/// </summary>
public abstract class BanCommandBase : CommandBase
{
    protected readonly BanManager _banManager;
    protected readonly MuteManager _muteManager;
    protected readonly GagManager _gagManager;
    protected readonly WarnManager _warnManager;
    protected readonly AdminDbManager _adminDbManager;
    protected readonly PlayerIpDbManager _playerIpDbManager;
    protected readonly PlayerSessionManager _playerSessionManager;
    protected readonly RecentPlayersTracker _recentPlayersTracker;
    protected readonly DiscordBotService _discord;
    protected readonly SanctionMenuConfig _sanctions;
    protected readonly MultiServerConfig _multiServerConfig;
    protected readonly int _banType;
    protected readonly PlayerSanctionStateService _sanctionStateService;

    protected BanCommandBase(
        ISwiftlyCore core,
        BanManager banManager,
        MuteManager muteManager,
        GagManager gagManager,
        WarnManager warnManager,
        AdminDbManager adminDbManager,
        AdminLogManager adminLogManager,
        PlayerIpDbManager playerIpDbManager,
        PlayerSessionManager playerSessionManager,
        RecentPlayersTracker recentPlayersTracker,
        DiscordBotService discord,
        PermissionsConfig permissions,
        CommandsConfig commands,
        TagsConfig tags,
        MessagesConfig messages,
        SanctionMenuConfig sanctions,
        MultiServerConfig multiServerConfig,
        int banType,
        PlayerSanctionStateService sanctionStateService,
        PermissionService permissionService)
        : base(core, permissions, commands, tags, messages, adminLogManager, permissionService)
    {
        _banManager = banManager;
        _muteManager = muteManager;
        _gagManager = gagManager;
        _warnManager = warnManager;
        _adminDbManager = adminDbManager;
        _playerIpDbManager = playerIpDbManager;
        _playerSessionManager = playerSessionManager;
        _recentPlayersTracker = recentPlayersTracker;
        _discord = discord;
        _sanctions = sanctions;
        _multiServerConfig = multiServerConfig;
        _banType = banType is >= 1 and <= 3 ? banType : 1;
        _sanctionStateService = sanctionStateService;
    }

    protected string T(string key, string fallback, params object[] args)
    {
        try
        {
            var value = args.Length == 0 ? L(key) : L(key, args);
            return string.Equals(value, key, StringComparison.OrdinalIgnoreCase)
                ? (args.Length == 0 ? fallback : string.Format(fallback, args))
                : value;
        }
        catch
        {
            return args.Length == 0 ? fallback : string.Format(fallback, args);
        }
    }

    protected bool ResolveGlobalMode()
    {
        if (!_multiServerConfig.Enabled)
        {
            return false;
        }

        return _multiServerConfig.GlobalBansByDefault;
    }

    protected BanApplyMode ResolveBanApplyMode(bool ipMode)
    {
        if (ipMode)
        {
            return BanApplyMode.Ip;
        }

        return _banType switch
        {
            2 => BanApplyMode.Ip,
            3 => BanApplyMode.Steam | BanApplyMode.Ip,
            _ => BanApplyMode.Steam
        };
    }

    protected static bool TryNormalizeIpTarget(string input, out string normalizedIp)
    {
        normalizedIp = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        if (!IPAddress.TryParse(input.Trim(), out var parsed))
        {
            return false;
        }

        normalizedIp = parsed.ToString();
        return true;
    }

    protected async Task AddOfflineSteamBanAsync(
        ICommandContext context,
        ulong targetSteamId,
        int duration,
        string reason,
        string adminName,
        ulong adminSteamId,
        bool isGlobal)
    {
        var existingBan = await _banManager.GetActiveBanFreshAsync(targetSteamId, null, _multiServerConfig.Enabled);
        if (existingBan != null)
        {
            await OnMainThreadAsync(() => Reply(context, "steamid_already_banned", targetSteamId));
            return;
        }

        _banManager.SetAdminContext(adminName, adminSteamId);
        var ok = await _banManager.AddBanAsync(targetSteamId, targetSteamId.ToString(), duration, reason, isGlobal);
        if (!ok)
        {
            await OnMainThreadAsync(() => Reply(context, "addban_failed"));
            return;
        }

        var durationDisplay = duration <= 0 ? L("permanent") : L("duration_minutes", duration);
        await OnMainThreadAsync(() => Reply(context, "addban_success", targetSteamId, durationDisplay));

        _ = AdminLogManager.AddLogAsync("addban", adminName, adminSteamId, targetSteamId, null, $"duration={duration};global={isGlobal};reason={reason}", null, null, reason);
    }

    protected async Task<bool> AddOfflineIpBanAsync(
        ICommandContext context,
        string ipAddress,
        int duration,
        string reason,
        string adminName,
        ulong adminSteamId,
        bool isGlobal,
        bool notifyResult = true)
    {
        if (!TryNormalizeIpTarget(ipAddress, out var normalizedIp))
        {
            if (notifyResult)
                await OnMainThreadAsync(() => ReplyRaw(context, T("invalid_ip", "Invalid IP address.")));
            return false;
        }

        var existing = await _banManager.GetActiveBanFreshAsync(0, normalizedIp, _multiServerConfig.Enabled);
        if (existing != null)
        {
            if (notifyResult)
                await OnMainThreadAsync(() => Reply(context, "lastban_ip_already_banned", normalizedIp));
            return false;
        }

        _banManager.SetAdminContext(adminName, adminSteamId);
        var ok = await _banManager.AddIpBanAsync(normalizedIp, normalizedIp, duration, reason, isGlobal);
        if (!ok)
        {
            if (notifyResult)
                await OnMainThreadAsync(() => ReplyRaw(context, T("ipban_failed", "IP ban failed (database error).")));
            return false;
        }

        if (notifyResult)
            await OnMainThreadAsync(() => ReplyRaw(context, T("ipban_success", "IP {0} banned successfully.", normalizedIp)));

        _ = AdminLogManager.AddLogAsync("ipban", adminName, adminSteamId, null, normalizedIp, $"duration={duration};global={isGlobal};reason={reason}", null, null, reason);

        return true;
    }

    protected sealed record OnlineTargetSnapshot(int PlayerId, ulong SteamId, string Name, string? IpAddress);

    [Flags]
    protected enum BanApplyMode
    {
        None = 0,
        Steam = 1,
        Ip = 2
    }
}
