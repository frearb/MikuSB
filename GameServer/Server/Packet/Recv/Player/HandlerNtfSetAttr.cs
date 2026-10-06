using MikuSB.Database;
using MikuSB.Data;
using MikuSB.Proto;

namespace MikuSB.GameServer.Server.Packet.Recv.Login;

[Opcode(CmdIds.NtfSetAttr)]
public class HandlerNtfSetAttr : Handler
{
    public override async Task OnHandle(Connection connection, byte[] data, ushort seqNo)
    {
        var req = NtfSetAttr.Parser.ParseFrom(data);
        var player = connection.Player!;
        // Exchange counters and their reset marker are owned by the server.
        if (req.Gid == AttrIds.CashExchange.GroupId && req.Sid is
            AttrIds.CashExchange.VigorLimitSid or AttrIds.CashExchange.SilverLimitSid or AttrIds.CashExchange.RefreshDaySid)
            return;
        var viewed = req.Gid == AttrIds.House.Gid &&
            (req.Sid >= AttrIds.House.SuitViewedStartSid && req.Sid < AttrIds.House.SuitViewedEndSid ||
             req.Sid == AttrIds.House.RingViewedSid || req.Sid == AttrIds.House.GirlRingViewedSid ||
             req.Sid == AttrIds.House.HasOpenedPuzzleThisWeekSid);
        // The client builds these bitsets from its cached value. Browsing another
        // entry must never erase earlier read bits, even when that cache is stale.
        if (viewed)
            req.Val |= player.Attributes.GetValue(req.Gid, req.Sid);

        player.Attributes.Set(req.Gid, req.Sid, req.Val);
        DatabaseHelper.SaveDatabaseType(player.Data);
        if (viewed)
        {
            // NtfSetAttr is the client's write request. Return the merged state
            // through the same ExtraSync path used by login and House callbacks.
            var sync = new NtfSyncPlayer();
            player.Attributes.SyncTo(sync, req.Gid, req.Sid, req.Val);
            // House_Request is registered and safely ignores an empty argument.
            // An empty API still applies ExtraSync, but then raises a Lua dispatch error.
            await player.SendPacket(CmdIds.NtfScript, new NtfCallScript { Api = "House_Request", Arg = "{}", ExtraSync = sync });
        }

        await player.OnHeartBeat();
    }
}
