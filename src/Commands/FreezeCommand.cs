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

public class FreezeCommand : CommandBase
{
    private readonly AdminDbManager _adminDbManager;

    public FreezeCommand(
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
        RunTargetedFunCommand(context, CommandsConfig.Freeze, Permissions.Freeze, "freeze_usage", _adminDbManager, "Freeze",
            includeDeadPlayers: false,
            onMainThread: (ctx, args, targets, adminName) =>
            {
                int? durationSeconds = null;
                if (args.Length >= 2 && int.TryParse(args[1], out var parsedSeconds) && parsedSeconds > 0)
                {
                    durationSeconds = parsedSeconds;
                }

                foreach (var target in targets)
                {
                    var playerId = target.PlayerID;
                    PlayerUtils.Freeze(target);
                    FreezeSharedState.FrozenPlayers.Add(playerId);

                    if (FreezeSharedState.VisualPlayers.Add(playerId))
                    {
                        StartFreezeVisualPulse(target.SteamID);
                    }

                    if (durationSeconds.HasValue)
                    {
                        Core.Scheduler.DelayBySeconds(durationSeconds.Value, () =>
                        {
                            var player = Core.PlayerManager.GetAllPlayers().FirstOrDefault(p => p.IsValid && p.PlayerID == playerId);
                            if (player == null)
                            {
                                return;
                            }

                            if (FreezeSharedState.FrozenPlayers.Contains(playerId))
                            {
                                PlayerUtils.Unfreeze(player);
                                FreezeSharedState.FrozenPlayers.Remove(playerId);
                                FreezeSharedState.VisualPlayers.Remove(playerId);
                            }
                        });
                    }
                }

                foreach (var target in targets)
                {
                    PlayerUtils.SendNotification(Core, target, Messages,
                        $"<font color='#00ccff'><b>{L("frozen_personal_html")}</b></font><br><br>{L("label_by")}: <font color='#ffcc00'>{ResolveVisibleAdminName(target, adminName)}</font>",
                        $" \x02{L("prefix")}\x01 {L("frozen_personal_chat", ResolveVisibleAdminName(target, adminName))}");
                }

                BroadcastNotification(adminName, "freeze_notification", FormatTargetName(targets));

                var freezeTargetSteamIds = string.Join(",", targets.Select(t => t.SteamID));
                _ = AdminLogManager.AddLogAsync("freeze", adminName, ctx.Sender?.SteamID ?? 0, null, null, $"targets={freezeTargetSteamIds};count={targets.Count};duration={durationSeconds?.ToString() ?? "0"}");
            });
    }

    private void StartFreezeVisualPulse(ulong steamId)
    {
        Core.Scheduler.DelayBySeconds(1.0f, () =>
        {
            var player = Core.PlayerManager.GetAllPlayers().FirstOrDefault(p => p.IsValid && p.SteamID == steamId);
            if (player == null)
            {
                return;
            }

            var playerId = player.PlayerID;
            if (!FreezeSharedState.FrozenPlayers.Contains(playerId))
            {
                FreezeSharedState.VisualPlayers.Remove(playerId);
                return;
            }

            var pawn = player.PlayerPawn;
            if (pawn?.IsValid != true)
            {
                return;
            }

            if (!FreezeSharedState.OriginalViewmodelFov.ContainsKey(playerId))
            {
                FreezeSharedState.OriginalViewmodelFov[playerId] = pawn.ViewmodelFOV > 0 ? pawn.ViewmodelFOV : 68f;
            }

            if (!FreezeSharedState.OriginalViewmodelOffsets.ContainsKey(playerId))
            {
                FreezeSharedState.OriginalViewmodelOffsets[playerId] = (pawn.ViewmodelOffsetX, pawn.ViewmodelOffsetY, pawn.ViewmodelOffsetZ);
            }

            pawn.ViewmodelFOV = 40f;
            pawn.ViewmodelFOVUpdated();
            pawn.ViewmodelOffsetX = -10f;
            pawn.ViewmodelOffsetY = -10f;
            pawn.ViewmodelOffsetZ = -10f;

            StartFreezeVisualPulse(steamId);
        });
    }
}


