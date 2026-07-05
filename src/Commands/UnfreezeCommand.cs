using CS2_Admin.Database;
using CS2_Admin.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Natives;

using CS2_Admin.Services;
using CS2_Admin.Config;
namespace CS2_Admin.Commands;

public class UnfreezeCommand : CommandBase
{
    private readonly AdminDbManager _adminDbManager;

    public UnfreezeCommand(
        ISwiftlyCore core,
        AdminDbManager adminDbManager,
        PermissionsConfig permissions,
        CommandsConfig commandsConfig,
        TagsConfig tags,
        MessagesConfig messages,
        AdminLogManager adminLogManager,
        PermissionService permissionService) : base(core, permissions, commandsConfig, tags, messages, adminLogManager, permissionService)
    {
        _adminDbManager = adminDbManager;
    }

    public override void Execute(ICommandContext context)
    {
        RunTargetedFunCommand(context, CommandsConfig.Unfreeze, Permissions.Unfreeze, "unfreeze_usage", _adminDbManager, "Unfreeze",
            onMainThread: (ctx, args, targets, adminName) =>
            {
                foreach (var target in targets)
                {
                    PlayerUtils.Unfreeze(target);
                    var playerId = target.PlayerID;
                    FreezeSharedState.FrozenPlayers.Remove(playerId);
                    FreezeSharedState.VisualPlayers.Remove(playerId);
                    RestoreFreezeVisuals(playerId);
                }

                foreach (var target in targets)
                {
                    PlayerUtils.SendNotification(Core, target, Messages,
                        $"<font color='#00ff00'><b>{L("unfrozen_personal_html")}</b></font><br><br>{L("label_by")}: <font color='#ffcc00'>{ResolveVisibleAdminName(target, adminName)}</font>",
                        $" \x02{L("prefix")}\x01 {L("unfrozen_personal_chat", ResolveVisibleAdminName(target, adminName))}");
                }

                BroadcastNotification(adminName, "unfreeze_notification", FormatTargetName(targets));

                var unfreezeTargetSteamIds = string.Join(",", targets.Select(t => t.SteamID));
                _ = AdminLogManager.AddLogAsync("unfreeze", adminName, ctx.Sender?.SteamID ?? 0, null, null, $"targets={unfreezeTargetSteamIds};count={targets.Count}");
            });
    }

    private void RestoreFreezeVisuals(int playerId)
    {
        if (FreezeSharedState.OriginalViewmodelFov.TryGetValue(playerId, out var originalFov))
        {
            var player = Core.PlayerManager.GetAllPlayers().FirstOrDefault(p => p.IsValid && p.PlayerID == playerId);
            if (player?.PlayerPawn?.IsValid == true)
            {
                player.PlayerPawn.ViewmodelFOV = originalFov;
                player.PlayerPawn.ViewmodelFOVUpdated();
            }

            FreezeSharedState.OriginalViewmodelFov.Remove(playerId);
        }

        if (FreezeSharedState.OriginalViewmodelOffsets.TryGetValue(playerId, out var originalOffsets))
        {
            var player = Core.PlayerManager.GetAllPlayers().FirstOrDefault(p => p.IsValid && p.PlayerID == playerId);
            if (player?.PlayerPawn?.IsValid == true)
            {
                player.PlayerPawn.ViewmodelOffsetX = originalOffsets.X;
                player.PlayerPawn.ViewmodelOffsetY = originalOffsets.Y;
                player.PlayerPawn.ViewmodelOffsetZ = originalOffsets.Z;
            }

            FreezeSharedState.OriginalViewmodelOffsets.Remove(playerId);
        }
    }
}


