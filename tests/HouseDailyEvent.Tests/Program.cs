using System.Text.Json.Nodes;
using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.House;
using MikuSB.GameServer.Game.Player;
using MikuSB.GameServer.Server.CallGS;
using MikuSB.GameServer.Server.CallGS.Handlers.House;
using MikuSB.Proto;
using MikuSB.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

const uint gid = AttrIds.House.Gid;
var before = DateTimeOffset.Parse("2026-10-05T03:59:59+08:00");
var after = before.AddSeconds(1);
var day = HouseDailyEventService.Day(after, 4);
Assert(day == HouseDailyEventService.Day(before, 4) + 1, "Reset must occur at 04:00 UTC+8");
Assert(day == HouseDailyEventService.Day(after.ToUniversalTime(), 4), "Day must be independent of host timezone");

var data = new PlayerGameData();
var attrs = new PlayerAttributes(data);
var events = new[] { new HouseDailyTalkExcel { GirlId = 1, StoryId = 16, StoryKey = "test" } };
attrs.Set(gid, 53, 1);
attrs.Set(gid, 103, 2);
var sync = new NtfSyncPlayer();
Assert(HouseDailyEventService.Refresh(attrs, day, events, _ => true, sync), "First refresh must create an event");
Assert(attrs.GetValue(gid, 53) == 16 && attrs.GetValue(gid, 103) == 0, "Only one girl may have today's event");
Assert(sync.Custom[attrs.ToShiftedAttrKey(gid, 103)] == 0, "Expired events must sync their zero value");
Assert(!HouseDailyEventService.Refresh(attrs, day, [], _ => true), "Same day must never reroll");
var reloaded = JsonConvert.DeserializeObject<PlayerGameData>(JsonConvert.SerializeObject(data))!;
Assert(!HouseDailyEventService.Refresh(new PlayerAttributes(reloaded), day, [], _ => true), "Saved state must survive reconnect/restart");

for (uint story = 1; story <= 45; story++)
{
    HouseDailyEventService.Consume(attrs, 1, story, new NtfSyncPlayer());
    Assert(HouseDailyEventService.Read(attrs, 1, story), $"Read bit must match Lua for story {story}");
}
Assert(!HouseDailyEventService.Read(attrs, 1, 0) && !HouseDailyEventService.Read(attrs, 1, 46), "Invalid indices must not wrap shifts");
Assert(!HouseDailyEventService.Refresh(attrs, day, events, _ => true), "Consumption must not allow another event today");
Assert(HouseDailyEventService.Refresh(attrs, day + 1, events, _ => true), "Next day must refresh");
Assert(HouseDailyEventService.Read(attrs, 1, 45), "Daily refresh must retain permanent history");
Assert(!HouseDailyEventService.Refresh(attrs, day + 2, events, _ => false), "No eligible girl must expire old event");
Assert(attrs.GetValue(gid, 53) == 0, "Ineligible next day must not keep yesterday's event");
Assert(HouseDailyEventService.Refresh(attrs, day + 2, events, _ => true), "Moving a girl in later must allow the day's event");

var ownedGirls = new HashSet<uint> { 1 };
var ownedItems = new HashSet<ulong>();
var row = events[0];
Assert(!Eligible(row), "Girl without a room must not receive an unreachable event");
attrs.Set(gid, 51, 100);
Assert(!Eligible(row), "Registered but roomless girl must be excluded");
attrs.Set(gid, 51, 1);
Assert(Eligible(row), "Owned girl living in a room should qualify");
row.PreLevel = new JValue(123);
Assert(!Eligible(row), "Locked prerequisite level must be excluded");
attrs.Set(AttrIds.Quest.LevelPassGid, 123, 1);
Assert(Eligible(row), "Completed prerequisite level should qualify");
row.PreStory = JArray.Parse("[2,16]");
Assert(!Eligible(row), "Unread prerequisite event must be excluded");
HouseDailyEventService.Consume(attrs, 2, 16, new NtfSyncPlayer());
Assert(Eligible(row), "Read prerequisite event should qualify");
row.NeedItem = JArray.Parse("[7,1,3,2]");
Assert(!Eligible(row), "Missing required skin must exclude event");
ownedItems.Add(GameResourceTemplateId.FromGdpl(7, 1, 3, 2));
Assert(Eligible(row), "Owned required skin should qualify");

new HouseSupportFurnitureExcel { AreaId = 1, Index = 1, FurnitureTmpId = 101 }.Loaded();
new HouseSupportFurnitureExcel { AreaId = 1, Index = 2, FurnitureTmpId = 102 }.Loaded();
new HouseFurniturePosExcel { AreaId = 1, GroupId = 1, FurnitureTmpId = JArray.Parse("[101,102]") }.Loaded();
new HouseFurniturePosExcel { AreaId = 2, GroupId = 1, FurnitureTmpId = JArray.Parse("[201,202]") }.Loaded();
Assert(GameData.HouseFurniturePosData.Count == 2, "Different areas' replacement groups must not collide");
row.Furnitures = JArray.Parse("[101]");
Assert(!Eligible(row), "Ungifted furniture must exclude event");
attrs.Set(gid, 60, 1u << 3);
attrs.Set(gid, 70, 1);
Assert(Eligible(row), "Gifted visible furniture should qualify");
attrs.Set(gid, 73, 1u << 1);
Assert(!Eligible(row), "Folded furniture must exclude event");
attrs.Set(gid, 73, 0);
attrs.Set(gid, 70, 2);
Assert(!Eligible(row), "An unselected replacement must be hidden if all group members are unfolded");
attrs.Set(gid, 73, 1u << 2);
Assert(Eligible(row), "When another replacement is folded, unfolded target becomes visible");
row.BeginTime = new JValue("2026-10-06 00:00:00");
Assert(!Eligible(row), "Future event must not qualify");
row.BeginTime = null;

// Test the actual House callback without starting a server or opening a database.
ConfigManager.Config.ServerOption.EnableHouseDailyRandomEvent = false;
GameData.HouseDailyTalkData.Clear();
row.Loaded();
var player = new PlayerInstance(data);
var context = new CallGSContext { Connection = null!, Player = player, RawParam = "{}", SequenceNumber = 0 };
player.Attributes.Set(gid, 53, 16);
var handler = new DoDailyTalk();
var result = await handler.Handle(context, "{\"GirlId\":1,\"EventId\":999}");
var response = JsonNode.Parse(result.Argument)!;
Assert(response["FuncName"]!.GetValue<string>() == "DoDailyTalk" && response["EventId"]!.GetValue<uint>() == 16,
    "Handler must return the persisted event rather than a supplied event ID");
Assert(result.Sync!.Custom.Count > 0 && player.Attributes.GetValue(gid, 53) == 0, "Handler must consume and sync event");
result = await handler.Handle(context, "{\"GirlId\":1}");
Assert(JsonNode.Parse(result.Argument)!["EventId"]!.GetValue<int>() == 0, "Repeat request must not play a second event");
result = await handler.Handle(context, "{\"GirlId\":1,\"EventId\":16,\"Review\":true}");
Assert(JsonNode.Parse(result.Argument)!["Review"]!.GetValue<bool>(), "Read event can be reviewed");
Assert(player.Attributes.GetValue(gid, AttrIds.House.DailyTalkRefreshDaySid) == day + 2, "Review must not reset daily marker");

// Load deployed JSON, including optional fields represented as empty strings.
var resourceRoot = Path.GetFullPath("MikuSB/bin/Debug/net10.0/Resources/house");
var deployed = JsonConvert.DeserializeObject<List<HouseDailyTalkExcel>>(File.ReadAllText(Path.Combine(resourceRoot, "daily_talk.json")))!;
Assert(deployed.Count > 100 && deployed.All(x => x.GetId() > 0), "Real daily_talk resources must deserialize");
foreach (var entry in deployed) entry.Loaded();
Assert(GameData.HouseDailyTalkData.Count == deployed.Count, "Every girl/story pair must be loaded");
var deployedFurniture = JsonConvert.DeserializeObject<List<HouseSupportFurnitureExcel>>(
    File.ReadAllText(Path.Combine(resourceRoot, "support_furniture.json")))!;
foreach (var entry in deployedFurniture) entry.Loaded();
var deployedGroups = JsonConvert.DeserializeObject<List<HouseFurniturePosExcel>>(
    File.ReadAllText(Path.Combine(resourceRoot, "FurniturePos.json")))!;
foreach (var entry in deployedGroups) entry.Loaded();
Assert(deployedFurniture.Count > 100 && deployedGroups.Count > 1, "Real furniture resources must load");
Console.WriteLine($"House daily event checks passed ({deployed.Count} deployed events).");

bool Eligible(HouseDailyTalkExcel entry) => HouseDailyEventService.Eligible(attrs, entry, ownedGirls, ownedItems, after);
static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
