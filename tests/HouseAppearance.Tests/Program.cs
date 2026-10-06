using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using MikuSB.Data;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.GameServer.Server;
using MikuSB.GameServer.Server.Packet.Recv.Login;
using MikuSB.Proto;
using MikuSB.TcpSharp;
using Newtonsoft.Json;

// Exercise the real packet handler and server reply without opening the runtime database.
using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
listener.Listen(1);
using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
await client.ConnectAsync(listener.LocalEndPoint!);
using var server = await listener.AcceptAsync();
var connection = new TestConnection(server, (IPEndPoint)client.LocalEndPoint!);
var data = new PlayerGameData { Uid = 12345 };
var player = new PlayerInstance(data) { Connection = connection };
connection.Player = player;
using var stream = new NetworkStream(client, ownsSocket: false);
var handler = new HandlerNtfSetAttr();
const uint gid = AttrIds.House.Gid;

try
{
    // Recorded client values overwrite each other with the old handler.
    await Update(AttrIds.House.SuitViewedStartSid, 19, 19);
    await Update(AttrIds.House.SuitViewedStartSid, 23, 23);
    await Update(AttrIds.House.SuitViewedStartSid, 27, 31);
    await Update(AttrIds.House.SuitViewedStartSid, 23, 31);
    await Update(AttrIds.House.GirlRingViewedSid, 33798, 33798);
    await Update(AttrIds.House.GirlRingViewedSid, 132102, 164870);
    await Update(AttrIds.House.GirlRingViewedSid, 1062, 164902);
    // Latest real session: all configured ring bits are saved, but the client
    // kept sending 132134 after receiving the old NtfSetAttr replies.
    await Update(AttrIds.House.GirlRingViewedSid, 181542, 181542);
    await Update(AttrIds.House.GirlRingViewedSid, 132134, 181542);
    await Update(AttrIds.House.RingViewedSid, 1, 1);
    await Update(AttrIds.House.RingViewedSid, 0, 1);
    for (uint sid = AttrIds.House.SuitViewedStartSid + 1; sid < AttrIds.House.SuitViewedEndSid; sid++)
    {
        await Update(sid, 1, 1);
        await Update(sid, 2, 3);
        await Update(sid, 0, 3);
    }

    // Unrelated attributes still support replacement and clearing, including range boundaries.
    await Replace(gid, AttrIds.House.SuitViewedStartSid - 1);
    await Replace(gid, AttrIds.House.SuitViewedEndSid);
    await Replace(gid, 7);
    await Replace(gid + 1, AttrIds.House.SuitViewedStartSid);
    Assert(client.Available == 0, "Unrelated attributes must not produce new replies");

    await Update(AttrIds.House.HasOpenedPuzzleThisWeekSid, 1, 1);
    await Update(AttrIds.House.HasOpenedPuzzleThisWeekSid, 0, 1);

    var reloaded = new PlayerAttributes(JsonConvert.DeserializeObject<PlayerGameData>(JsonConvert.SerializeObject(data))!);
    Assert(reloaded.GetValue(gid, AttrIds.House.SuitViewedStartSid) == 31, "Suit history must survive reload");
    Assert(reloaded.GetValue(gid, AttrIds.House.GirlRingViewedSid) == 181542, "Ring history must survive reload");
    Console.WriteLine("PASS: appearance read bits accumulate, sync to client, and survive reload; other attributes retain replacement semantics");
}
finally
{
    connection.Stop();
}

async Task Update(uint sid, uint value, uint expected)
{
    await handler.OnHandle(connection, new NtfSetAttr { Gid = gid, Sid = sid, Val = value }.ToByteArray(), 0);
    Assert(player.Attributes.GetValue(gid, sid) == expected, $"Saved value for {sid}");
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var packet = await new PacketCodec().ReadPacketAsync(stream, timeout.Token);
    Assert(packet?.CmdId == CmdIds.NtfScript, "Read state must use the client ExtraSync path, not echo its write request");
    var reply = NtfCallScript.Parser.ParseFrom(packet!.Body.ToArray());
    Assert(reply.Api == "House_Request" && reply.Arg == "{}",
        "Attribute sync must dispatch through a registered handler without invoking a UI callback");
    Assert(reply.ExtraSync.Custom[player.Attributes.ToPackedAttrKey(gid, sid)] == expected &&
        reply.ExtraSync.Custom[player.Attributes.ToShiftedAttrKey(gid, sid)] == expected,
        "ExtraSync must contain accumulated read state under both supported attribute encodings");
}

async Task Replace(uint group, uint sid)
{
    player.Attributes.Set(group, sid, 3);
    await handler.OnHandle(connection, new NtfSetAttr { Gid = group, Sid = sid, Val = 1 }.ToByteArray(), 0);
    Assert(player.Attributes.GetValue(group, sid) == 1, "Normal attributes must replace values");
    await handler.OnHandle(connection, new NtfSetAttr { Gid = group, Sid = sid }.ToByteArray(), 0);
    Assert(player.Attributes.GetValue(group, sid) == 0, "Normal attributes must allow clearing");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

sealed class TestConnection(Socket socket, IPEndPoint remote) : Connection(socket, remote)
{
    public override void Start() { }
    public override void Stop(bool isServerStop = false) { IsOnline = false; }
}
