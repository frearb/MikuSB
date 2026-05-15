using MikuSB.Enums.Player;
using MikuSB.GameServer.Server.CallGS;
using MikuSB.GameServer.Server.CallGS.Handlers.Boss;
using MikuSB.Proto;

namespace MikuSB.GameServer.Command.Commands;

[CommandInfo("bosspvp", "BossPvp command.", "Usage: /bosspvp clear [@uid]", ["bpvp"], [PermEnum.Admin, PermEnum.Support])]
public class CommandBossPvp : ICommands
{
    [CommandMethod("clear")]
    public async ValueTask ClearLineupLocks(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;

        var player = arg.Target!.Player!;
        var sync = new NtfSyncPlayer();
        var count = BossPvpLogicState.ClearLineupLocks(player, sync);

        if (sync.CustomStr.Count > 0)
        {
            await BossPvpLogicState.SendStrAttrsAsync(arg.Target);
            await CallGSRouter.SendScript(arg.Target, "BossPvpLogic_ClearLineupLocks", $"{{\"nCount\":{count}}}", sync);
        }

        await arg.SendMsg($"Cleared {count} BossPvp lineup record(s) for UID {player.Uid}.");
    }
}
