using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using Google.Protobuf;
using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database;
using MikuSB.Database.Inventory;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Inventory;
using MikuSB.GameServer.Game.Player;
using MikuSB.GameServer.Server;
using MikuSB.GameServer.Server.CallGS;
using MikuSB.GameServer.Server.CallGS.Handlers.Cash;
using MikuSB.GameServer.Server.Packet.Recv.Login;
using MikuSB.Proto;
using MikuSB.TcpSharp;

var resourceDir = Path.GetFullPath("MikuSB/bin/Debug/net10.0/Resources");
foreach (var row in JsonSerializer.Deserialize<List<SilverExchangeExcel>>(
             File.ReadAllText(Path.Combine(resourceDir, "cash/silver_exchange.json")))!) row.Loaded();
foreach (var row in JsonSerializer.Deserialize<List<VigorExchangeExcel>>(
             File.ReadAllText(Path.Combine(resourceDir, "cash/vigor_exchange.json")))!) row.Loaded();
Assert(GameData.SilverExchangeData.Count == 20 && GameData.VigorExchangeData.Count == 10, "All exchange tiers must load");

var now = DateTimeOffset.Parse("2026-10-06T19:05:37+08:00");
var boundary = DateTimeOffset.Parse("2026-10-07T04:00:00+08:00");
Assert(CashExchangeService.Day(boundary.AddSeconds(-1)) + 1 == CashExchangeService.Day(boundary), "Daily reset must be at 04:00 UTC+8");
Assert(CashExchangeService.Day(boundary) == CashExchangeService.Day(boundary.ToUniversalTime()), "Reset must be timezone independent");
var player = MakePlayer(23456, 50000);
var request = JsonSerializer.Deserialize<CashExchangeParam>("""
{"nCost":10,"nExhcangTarget":3,"nExhcangeCash":2}
""")!;
var result = CashExchangeService.Exchange(player, request, now);
Assert(Success(result) && JsonNode.Parse(result.Argument)!["nCount"]!.GetValue<uint>() == 4680, "Recorded request must grant 4680 silver");
Assert(player.GetCurrencyBalance(2) == 49990 && player.GetCurrencyBalance(3) == 4680, "Exchange must spend actual item currency and grant silver");
Assert(result.Sync!.Items.Count == 2 && result.Sync.Custom[player.Attributes.ToShiftedAttrKey(70, 2)] == 1,
    "Reply must sync both inventory currencies and the exchange count");
Assert(result.Sync.Custom[player.Attributes.ToPackedAttrKey(70, 2)] == 1, "Both supported attribute key formats must sync");
Reject(request, "Stale price must fail");
Reject(new() { Source = 1, Target = 3, Cost = 20 }, "Other source currencies must fail");
Reject(new() { Source = 2, Target = 2, Cost = 20 }, "Unsupported target must fail");
Reject(new() { Source = 2, Target = 3 }, "Zero price must fail");
Assert(Success(CashExchangeService.Exchange(player, new() { Source = 2, Target = 3, Cost = 20 }, now)), "Second exchange must use next tier");
Assert(player.GetCurrencyBalance(3) == 13260, "Second tier must add 8580 silver");

var saved = JsonSerializer.Serialize(player.Data);
player = new PlayerInstance(JsonSerializer.Deserialize<PlayerGameData>(saved)!) { Uid = player.Uid, InventoryManager = player.InventoryManager };
CashExchangeService.Refresh(player.Attributes, now);
Assert(player.Attributes.GetValue(70, 2) == 2, "Reloaded persisted counters must survive reconnect");
for (uint i = 3; i <= 20; i++)
    Assert(Success(CashExchangeService.Exchange(player, new() { Source = 2, Target = 3, Cost = GameData.SilverExchangeData[i].Cost }, now)), "Every configured silver tier must work");
Reject(new() { Source = 2, Target = 3, Cost = 500 }, "21st silver exchange must fail");
var vigorBefore = player.Data.Vigor;
for (uint i = 1; i <= 10; i++)
    Assert(Success(CashExchangeService.Exchange(player, new() { Source = 2, Target = 4, Cost = GameData.VigorExchangeData[i].Cost }, now)), "Every configured vigor tier must work");
Assert(player.Data.Vigor == vigorBefore + 600 && player.Attributes.GetValue(70, 2) == 20, "Vigor must update core and use an independent counter");
Reject(new() { Source = 2, Target = 4, Cost = 180 }, "11th vigor exchange must fail");
CashExchangeService.Refresh(player.Attributes, boundary.AddSeconds(-1));
Assert(player.Attributes.GetValue(70, 1) == 10, "No early reset");
result = CashExchangeService.Exchange(player, request, boundary);
Assert(Success(result) && player.Attributes.GetValue(70, 2) == 1 && player.Attributes.GetValue(70, 1) == 0,
    "Next game day must reset both limits and restore first price");
Assert(result.Sync!.Custom[player.Attributes.ToShiftedAttrKey(70, 1)] == 0, "Reset must sync the other currency's count too");

player = MakePlayer(23457, 9);
CashExchangeService.Refresh(player.Attributes, now);
Reject(request, "Insufficient funds must not mutate balances or counts");
player = MakePlayer(23458, 100);
player.AddCurrency(3, uint.MaxValue, new());
CashExchangeService.Refresh(player.Attributes, now);
Reject(request, "Silver overflow must fail before spending");
player.Data.Vigor = uint.MaxValue;
Reject(new() { Source = 2, Target = 4, Cost = 30 }, "Vigor overflow must fail before spending");
player = MakePlayer(23459, 100);
player.Attributes.Set(70, 2, 3);
CashExchangeService.Refresh(player.Attributes, now);
Assert(player.Attributes.GetValue(70, 2) == 3, "First reset marker must preserve legacy counters");

// Replay the captured request through registration, dispatch and the protobuf response.
player = MakePlayer(23460, 100);
using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
listener.Listen(1);
using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
await client.ConnectAsync(listener.LocalEndPoint!);
using var server = await listener.AcceptAsync();
var connection = new TestConnection(server, (IPEndPoint)client.LocalEndPoint!) { Player = player };
player.Connection = connection;
using var stream = new NetworkStream(client, ownsSocket: false);
try
{
    SocketConnection.LogMap[CmdIds.NtfScript] = "NtfScript";
    CallGSRouter.Init();
    await CallGSRouter.Route(connection, new ReqCallGS
    {
        Api = "Cash_Exchange", Param = "{\"nCost\":10,\"nExhcangTarget\":3,\"nExhcangeCash\":2}"
    }, 0);
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var packet = await new PacketCodec().ReadPacketAsync(stream, timeout.Token);
    Assert(packet?.CmdId == CmdIds.NtfScript, "Registered handler must send NtfScript");
    var reply = NtfCallScript.Parser.ParseFrom(packet!.Body.ToArray());
    var arg = JsonNode.Parse(reply.Arg)!;
    Assert(reply.Api == "Cash_Exchange" && arg["nCashType"]!.GetValue<uint>() == 3 && arg["nCount"]!.GetValue<uint>() == 4680,
        "Response must match the client's GainItem callback");
    Assert(reply.ExtraSync.Items.Count == 2 && player.GetCurrencyBalance(2) == 90, "Wire response must include currency sync");
    await new HandlerNtfSetAttr().OnHandle(connection, new NtfSetAttr { Gid = 70, Sid = 2, Val = 0 }.ToByteArray(), 0);
    Assert(player.Attributes.GetValue(70, 2) == 1, "Client writes must not reset server exchange counters");
    player.Attributes.Set(70, AttrIds.CashExchange.RefreshDaySid, CashExchangeService.Day(DateTimeOffset.UtcNow) - 1);
    await player.OnHeartBeat();
    packet = await new PacketCodec().ReadPacketAsync(stream, timeout.Token);
    reply = NtfCallScript.Parser.ParseFrom(packet!.Body.ToArray());
    Assert(reply.Api == "House_Request" && reply.ExtraSync.Custom[player.Attributes.ToShiftedAttrKey(70, 2)] == 0,
        "Online heartbeat must sync daily reset without requiring reconnect");
}
finally { connection.Stop(); }
Console.WriteLine("PASS: recorded request routing, configured tiers, balances, sync, limits, persistence, daily reset, invalid requests and overflow");

void Reject(CashExchangeParam req, string message)
{
    var data = JsonSerializer.Serialize(player.Data);
    var inventory = JsonSerializer.Serialize(player.InventoryManager.InventoryData);
    Assert(!Success(CashExchangeService.Exchange(player, req, now)), message);
    Assert(data == JsonSerializer.Serialize(player.Data) && inventory == JsonSerializer.Serialize(player.InventoryManager.InventoryData), message + " (no mutation)");
}

static bool Success(CallGSResult result) => JsonNode.Parse(result.Argument)?["sErr"] == null;
static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
static PlayerInstance MakePlayer(int uid, uint gold)
{
    var data = new PlayerGameData { Uid = uid };
    var inventory = new InventoryData { Uid = uid };
    DatabaseHelper.UidInstanceMap[uid] = [data, inventory];
    var player = new PlayerInstance(data) { Uid = uid };
    player.InventoryManager = new InventoryManager(player);
    player.AddCurrency(2, gold, new());
    return player;
}
sealed class TestConnection(Socket socket, IPEndPoint remote) : Connection(socket, remote)
{
    public override void Start() { }
    public override void Stop(bool isServerStop = false) { IsOnline = false; }
}
