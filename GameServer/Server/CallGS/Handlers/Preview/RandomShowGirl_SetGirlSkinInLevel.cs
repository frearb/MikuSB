using MikuSB.Database.Player;
using MikuSB.Proto;
using System.Text.Json;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Preview;

[CallGSApi("RandomShowGirl_SetGirlSkinInLevel")]
public class RandomShowGirl_SetGirlSkinInLevel : ICallGSHandler
{
    private const uint GroupId = 139;
    private const uint SkinInLevelStartSid = 600;
    private const uint SkinInLevelEndSid = 900;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var skinIds = JsonSerializer.Deserialize<List<uint>>(param) ?? [];
        var player = connection.Player!;
        var sync = new NtfSyncPlayer();

        var oldAttrs = player.Data.Attrs
            .Where(x => x.Gid == GroupId && x.Sid >= SkinInLevelStartSid && x.Sid <= SkinInLevelEndSid)
            .ToList();

        foreach (var attr in oldAttrs)
        {
            AddSync(player, sync, attr.Sid, 0);
        }

        player.Data.Attrs.RemoveAll(x => x.Gid == GroupId && x.Sid >= SkinInLevelStartSid && x.Sid <= SkinInLevelEndSid);

        var sid = SkinInLevelStartSid;
        foreach (var skinId in skinIds.Where(x => x > 0).Take((int)(SkinInLevelEndSid - SkinInLevelStartSid + 1)))
        {
            player.Data.Attrs.Add(new PlayerAttr
            {
                Gid = GroupId,
                Sid = sid,
                Val = skinId
            });
            AddSync(player, sync, sid, skinId);
            sid++;
        }

        await CallGSRouter.SendScript(connection, "RandomShowGirl_SetGirlSkinInLevel", "{}", sync);
    }

    private static void AddSync(Game.Player.PlayerInstance player, NtfSyncPlayer sync, uint sid, uint value)
    {
        sync.Custom[player.ToPackedAttrKey(GroupId, sid)] = value;
        sync.Custom[player.ToShiftedAttrKey(GroupId, sid)] = value;
    }
}
