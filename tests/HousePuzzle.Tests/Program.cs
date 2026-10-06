using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net;
using System.Net.Sockets;
using MikuSB.GameServer.Server;
using MikuSB.TcpSharp;
using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.House;
using MikuSB.GameServer.Game.Inventory;
using MikuSB.GameServer.Game.Player;
using MikuSB.GameServer.Game.Reward;
using MikuSB.GameServer.Server.CallGS;
using MikuSB.GameServer.Server.CallGS.Handlers.House;
using MikuSB.Proto;
using Newtonsoft.Json.Linq;

const uint gid = AttrIds.House.Gid;
var monday = DateTimeOffset.Parse("2026-10-05T04:00:00+08:00");
Assert(HousePuzzleService.Week(monday.AddSeconds(-1)) + 7 == HousePuzzleService.Week(monday), "Weekly reset must occur Monday at 04:00 UTC+8");
Assert(HousePuzzleService.Week(monday) == HousePuzzleService.Week(monday.ToUniversalTime()), "Reset must be timezone independent");
var data = new PlayerGameData { Uid = 12346 };
var attrs = new PlayerAttributes(data);
HousePuzzleService.EnsureAvailable(attrs, monday);
Assert(attrs.GetValue(gid, AttrIds.House.PuzzleMapIdSid) == 1 && attrs.GetValue(gid, AttrIds.House.PuzzleRefreshTimesSid) == 0,
    "A free attempt must be reserved as an accessible current map, not an unusable counter");
Assert(attrs.GetValue(gid, AttrIds.House.HasOpenedPuzzleThisWeekSid) == 0, "Unopened free attempt must retain its dot");
var initial = JsonSerializer.Serialize(data);
HousePuzzleService.EnsureAvailable(attrs, monday.AddHours(1));
Assert(initial == JsonSerializer.Serialize(data), "Reconnect must not top up pieces or replace a map");
var solution = HousePuzzleService.ReadState("""
[{"ClipId":1,"RotId":1,"Pos":40},{"ClipId":1,"RotId":1,"Pos":42},
 {"ClipId":2,"RotId":1,"Pos":0},{"ClipId":4,"RotId":2,"Pos":22},
 {"ClipId":6,"RotId":1,"Pos":10},{"ClipId":5,"RotId":4,"Pos":32}]
""")!;
Assert(HousePuzzleService.Validate(attrs, solution, out var covered, out _) && covered == 24, "Starter pieces must allow full completion of the 6x4 board");
Assert(HousePuzzleService.ReadState("null") == null, "Null must not become a valid placement list");
Assert(!HousePuzzleService.Validate(attrs, [new() { ClipId = 99, RotId = 1 }], out _, out _), "Unknown piece must be rejected");
Assert(!HousePuzzleService.Validate(attrs, [new() { ClipId = 1, RotId = 1, Pos = 53 }], out _, out _), "Out of bounds must be rejected");
Assert(!HousePuzzleService.Validate(attrs, [solution[0], solution[0]], out _, out _), "Overlap must be rejected");
Assert(!HousePuzzleService.Validate(attrs, [new() { ClipId = 10, RotId = 1 }], out _, out _), "Unowned piece must be rejected");
Assert(HousePuzzleService.ReadState("{}") == null && HousePuzzleService.ReadState("[null]") is not null,
    "Malformed state must be rejected by parsing or validation");
Assert(!HousePuzzleService.Validate(attrs, HousePuzzleService.ReadState("[null]")!, out _, out _), "Null placement must be rejected");

// Exercise the actual callbacks and configured reward manager without a live database/server.
new OtherItemExcel { Genre = 4, Detail = 1, Particular = 6, Level = 5, LuaType = "cashbox", Param1Raw = new JValue(13) }.Loaded();
var player = new PlayerInstance(data) { Uid = data.Uid };
player.InventoryManager = new InventoryManager(player);
player.RewardManager = new RewardManager(player);
var context = new CallGSContext { Connection = null!, Player = player, RawParam = "{}", SequenceNumber = 0 };
var handler = new HousePuzzle();
var save = await handler.Handle(context, new JsonObject { ["FuncName"] = "SavePuzzleMapState", ["MapState"] = JsonNode.Parse(JsonSerializer.Serialize(solution)) }.ToJsonString());
Assert(JsonNode.Parse(save.Argument)!["bSuccess"]!.GetValue<bool>(), "Valid map state must save successfully");
Assert(player.Attributes.GetValue(gid, AttrIds.House.HasOpenedPuzzleThisWeekSid) == 1, "Using the puzzle must clear the unopened dot");
Assert(save.Sync!.CustomStr[player.Attributes.ToShiftedAttrKey(gid, AttrIds.House.PuzzleMapStateSid)].Contains("ClipId"), "Progress must sync to the client");
var reloaded = JsonSerializer.Deserialize<PlayerGameData>(JsonSerializer.Serialize(data))!;
var reloadAttrs = new PlayerAttributes(reloaded);
HousePuzzleService.EnsureAvailable(reloadAttrs, monday.AddHours(2));
Assert(HousePuzzleService.ReadState(reloadAttrs.GetStringValue(gid, AttrIds.House.PuzzleMapStateSid))!.Length == 6, "Saved progress must survive reconnect");
var finish = await handler.Handle(context, "{\"FuncName\":\"GetPuzzleMapReward\"}");
Assert(JsonNode.Parse(finish.Argument)!["FuncName"]!.GetValue<string>() == "GetPuzzleRewardSuccess", "Completion must use the actual client callback");
Assert(player.GetCurrencyBalance(13) == 120, "Full completion must grant 80 coverage + 40 completion reward");
Assert(player.Attributes.GetValue(gid, AttrIds.House.PuzzleMapIdSid) == 0, "Completion consumes the reserved attempt");
Assert(player.Attributes.GetValue(gid, AttrIds.House.PuzzleClipCountStartSid + 1) == 2, "Only used pieces must be consumed");
var duplicate = await handler.Handle(context, "{\"FuncName\":\"GetPuzzleMapReward\"}");
Assert(!JsonNode.Parse(duplicate.Argument)!["bSuccess"]!.GetValue<bool>() && player.GetCurrencyBalance(13) == 120, "Repeated claim must not grant twice");
var exhausted = JsonSerializer.Serialize(data);
HousePuzzleService.EnsureAvailable(player.Attributes, monday.AddDays(1));
Assert(exhausted == JsonSerializer.Serialize(data), "Consumed attempt must not refill in the same week");
HousePuzzleService.EnsureAvailable(player.Attributes, monday.AddDays(7));
Assert(player.Attributes.GetValue(gid, AttrIds.House.PuzzleMapIdSid) == 1 && player.Attributes.GetValue(gid, AttrIds.House.HasOpenedPuzzleThisWeekSid) == 0,
    "Next week must provide one new unopened attempt");
var partial = new[] { new PuzzlePlacement { ClipId = 1, RotId = 1, Pos = 0 } };
Assert(HousePuzzleService.Complete(player.Attributes, partial, new NtfSyncPlayer(), out var partialReward), "Valid partial puzzle must settle");
Assert(partialReward.Sum(x => x[4]) == 13, "Partial puzzle rewards must scale by actual covered cells");
player.Attributes.Set(gid, AttrIds.House.HasOpenedPuzzleThisWeekSid, 0);
HousePuzzleService.EnsureAvailable(player.Attributes, monday.AddDays(8));
Assert(player.Attributes.GetValue(gid, AttrIds.House.PuzzleMapIdSid) == 0, "Stale unread flag must not refill a previously granted weekly attempt");
player.Attributes.Set(gid, AttrIds.House.PuzzleRefreshTimesSid, 1);
var refresh = await handler.Handle(context, "{\"FuncName\":\"FreeRefreshMap\"}");
Assert(JsonNode.Parse(refresh.Argument)!["FuncName"]!.GetValue<string>() == "FreeRefreshMapSuccess" &&
    player.Attributes.GetValue(gid, AttrIds.House.PuzzleRefreshTimesSid) == 0, "Refresh callback must reserve an existing attempt");
var refreshed = JsonSerializer.Serialize(data);
var noRefresh = await handler.Handle(context, "{\"FuncName\":\"FreeRefreshMap\"}");
Assert(!JsonNode.Parse(noRefresh.Argument)!["bSuccess"]!.GetValue<bool>() && refreshed == JsonSerializer.Serialize(data),
    "Refreshing without attempts must retain the current board and pieces");
var badSave = await handler.Handle(context, "{\"FuncName\":\"SavePuzzleMapState\",\"MapState\":[{\"ClipId\":99,\"RotId\":1,\"Pos\":0}]}");
Assert(!JsonNode.Parse(badSave.Argument)!["bSuccess"]!.GetValue<bool>() && refreshed == JsonSerializer.Serialize(data),
    "Invalid save must not change persisted progress or pieces");

var opened = new PlayerAttributes(new PlayerGameData());
opened.Set(gid, AttrIds.House.HasOpenedPuzzleThisWeekSid, 1);
HousePuzzleService.EnsureAvailable(opened, monday);
Assert(opened.GetValue(gid, AttrIds.House.PuzzleMapIdSid) == 0, "Already opened exhausted account must not receive a free attempt");
var holes = new PlayerAttributes(new PlayerGameData());
holes.Set(gid, AttrIds.House.PuzzleMapIdSid, 2);
holes.Set(gid, AttrIds.House.PuzzleClipCountStartSid + 1, 1);
Assert(!HousePuzzleService.Validate(holes, [new() { ClipId = 1, RotId = 1, Pos = 30 }], out _, out _), "Pieces must not cover blocked board cells");

var friends = new PlayerAttributes(new PlayerGameData());
friends.Set(gid, AttrIds.House.PuzzleMapIdSid, 1);
friends.Set(gid, AttrIds.House.PuzzleClipCountStartSid + 1, (2u << 15) | 1u);
Assert(HousePuzzleService.Complete(friends, [new() { ClipId = 1, RotId = 1, Pos = 0 }, new() { ClipId = 1, RotId = 1, Pos = 20 }], new NtfSyncPlayer(), out _),
    "Friend pieces must count toward ownership");
Assert(friends.GetValue(gid, AttrIds.House.PuzzleClipCountStartSid + 1) == 1u << 15, "Consume local pieces first and retain unused friend pieces");

// Replay the actual 00:25:46 client request through House_Request and its network
// response. Calling HousePuzzle directly alone cannot detect missing registrations.
using var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
listener.Listen(1);
using var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
await client.ConnectAsync(listener.LocalEndPoint!);
using var server = await listener.AcceptAsync();
var wireData = JsonSerializer.Deserialize<PlayerGameData>(initial)!;
wireData.Uid = 12347;
var wirePlayer = new PlayerInstance(wireData) { Uid = wireData.Uid };
wirePlayer.InventoryManager = new InventoryManager(wirePlayer);
wirePlayer.RewardManager = new RewardManager(wirePlayer);
var wireConnection = new TestConnection(server, (IPEndPoint)client.LocalEndPoint!) { Player = wirePlayer };
wirePlayer.Connection = wireConnection;
using var stream = new NetworkStream(client, ownsSocket: false);
try
{
    SocketConnection.LogMap[CmdIds.NtfScript] = "NtfScript";
    await new House_Request().Handle(wireConnection, """
    {"MapState":[{"RotId":1,"ClipId":1,"Pos":2},{"RotId":1,"ClipId":1,"Pos":22},
    {"RotId":1,"ClipId":1,"Pos":42},{"RotId":1,"ClipId":1,"Pos":0},
    {"RotId":2,"ClipId":2,"Pos":21}],"FuncName":"GetPuzzleMapReward"}
    """, 0);
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    var packet = await new PacketCodec().ReadPacketAsync(stream, timeout.Token);
    Assert(packet?.CmdId == CmdIds.NtfScript, "Routed claim must reply with NtfScript");
    var reply = NtfCallScript.Parser.ParseFrom(packet!.Body.ToArray());
    Assert(reply.Api == "House_Request" && JsonNode.Parse(reply.Arg)!["FuncName"]!.GetValue<string>() == "GetPuzzleRewardSuccess",
        "Actual request must reach settlement and the registered client callback");
    Assert(wirePlayer.GetCurrencyBalance(13) == 66, "Recorded 20/24-cell layout must grant 66 coverage reward");
    Assert(reply.ExtraSync.Custom[wirePlayer.Attributes.ToShiftedAttrKey(gid, AttrIds.House.PuzzleMapIdSid)] == 0,
        "Reply must sync consumption of the current map");
}
finally { wireConnection.Stop(); }
Console.WriteLine("PASS: weekly grant, accessible board, solvable starter pieces, progress persistence, validation, rewards, consumption, and repeat-claim protection");

static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

sealed class TestConnection(Socket socket, IPEndPoint remote) : Connection(socket, remote)
{
    public override void Start() { }
    public override void Stop(bool isServerStop = false) { IsOnline = false; }
}
