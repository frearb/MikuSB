using MikuSB.Database;
using MikuSB.Proto;

namespace MikuSB.GameServer.Server.Packet.Recv.Login;

[Opcode(CmdIds.NtfSetAttr)]
public class HandlerNtfSetAttr : Handler
{
    public override async Task OnHandle(Connection connection, byte[] data, ushort seqNo)
    {
        var req = NtfSetAttr.Parser.ParseFrom(data);
        var player = connection.Player!;
        player.Attributes.Set(req.Gid, req.Sid, req.Val);
        DatabaseHelper.SaveDatabaseType(player.Data);
        await player.OnHeartBeat();
    }
}
