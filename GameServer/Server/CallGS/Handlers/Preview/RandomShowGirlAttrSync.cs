using MikuSB.Database.Player;
using MikuSB.Proto;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Preview;

internal static class RandomShowGirlAttrSync
{
    public static void ClearRange(Game.Player.PlayerInstance player, NtfSyncPlayer sync, uint gid, uint startSid, uint endSid)
    {
        var oldAttrs = player.Data.Attrs
            .Where(x => x.Gid == gid && x.Sid >= startSid && x.Sid <= endSid)
            .ToList();

        foreach (var attr in oldAttrs)
        {
            player.Attributes.SyncTo(sync, gid, attr.Sid, 0);
        }

        player.Data.Attrs.RemoveAll(x => x.Gid == gid && x.Sid >= startSid && x.Sid <= endSid);
    }

    public static void SetRange(Game.Player.PlayerInstance player, NtfSyncPlayer sync, uint gid, uint startSid, uint endSid, IReadOnlyList<uint> values)
    {
        var maxCount = (int)(endSid - startSid + 1);
        var count = Math.Min(values.Count, maxCount);

        for (var i = 0; i < count; i++)
        {
            var value = values[i];
            if (value == 0)
                continue;

            SetAttr(player, sync, gid, startSid + (uint)i, value);
        }
    }

    public static void SetAttr(Game.Player.PlayerInstance player, NtfSyncPlayer sync, uint gid, uint sid, uint value)
    {
        var attr = player.Data.Attrs.FirstOrDefault(x => x.Gid == gid && x.Sid == sid);
        if (attr == null)
        {
            attr = new PlayerAttr { Gid = gid, Sid = sid };
            player.Data.Attrs.Add(attr);
        }

        attr.Val = value;
        player.Attributes.SyncTo(sync, gid, sid, value);
    }
}
