using System.Globalization;
using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using MikuSB.Util;
using Newtonsoft.Json.Linq;

namespace MikuSB.GameServer.Game.House;

internal static class HouseDailyEventService
{
    private const uint Gid = AttrIds.House.Gid;

    internal static uint Day(DateTimeOffset now, int resetHour) =>
        (uint)DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(8))
            .AddHours(-Math.Clamp(resetHour, 0, 23)).DateTime).DayNumber;

    internal static bool Refresh(PlayerInstance player, NtfSyncPlayer? sync = null)
    {
        if (!ConfigManager.Config.ServerOption.EnableHouseDailyRandomEvent || GameData.HouseDailyTalkData.Count == 0)
            return false;
        var now = DateTimeOffset.UtcNow;
        var day = Day(now, ConfigManager.Config.ServerOption.HouseDailyRandomEventResetHour);
        if (player.Attributes.GetValue(Gid, AttrIds.House.DailyTalkRefreshDaySid) >= day)
            return false;

        var ownedGirls = player.CharacterManager.CharacterData.Characters
            .Select(x => (uint)((x.TemplateId >> 16) & 0xffff)).ToHashSet();
        var inventory = player.InventoryManager.InventoryData;
        var ownedItems = inventory.Items.Values.Where(x => x.ItemCount > 0).Select(x => x.TemplateId)
            .Concat(inventory.Skins.Values.Select(x => x.TemplateId))
            .Concat(inventory.Weapons.Values.Select(x => x.TemplateId))
            .Concat(player.CharacterManager.CharacterData.Characters.Select(x => x.TemplateId)).ToHashSet();
        return Refresh(player.Attributes, day, GameData.HouseDailyTalkData.Values,
            row => Eligible(player.Attributes, row, ownedGirls, ownedItems, now, player.Data.CompleteAllQuestLevels),
            sync);
    }

    internal static bool Refresh(PlayerAttributes attrs, uint day, IEnumerable<HouseDailyTalkExcel> events,
        Func<HouseDailyTalkExcel, bool> eligible, NtfSyncPlayer? sync = null, Random? random = null)
    {
        if (attrs.GetValue(Gid, AttrIds.House.DailyTalkRefreshDaySid) >= day)
            return false;
        var candidates = events.Where(eligible).GroupBy(x => x.GirlId).Select(x => x.ToArray()).ToArray();
        // Expire yesterday's event even if nobody is currently eligible.
        foreach (var attr in attrs.All.Where(x => x.Gid == Gid && x.Sid is >= 53 and <= 2503
                     && x.Sid % 50 == 3 && x.Val > 0).ToArray())
            Set(attrs, attr.Sid, 0, sync);
        Set(attrs, AttrIds.House.DailyTalkGirlSid, 0, sync);
        if (candidates.Length == 0)
            return false; // Retry after a girl moves in or unlock conditions change.

        random ??= Random.Shared;
        var girlEvents = candidates[random.Next(candidates.Length)];
        var chosen = girlEvents[random.Next(girlEvents.Length)];
        Set(attrs, chosen.GirlId * 50 + 3, chosen.StoryId, sync);
        Set(attrs, AttrIds.House.DailyTalkGirlSid, chosen.GirlId, sync);
        Set(attrs, AttrIds.House.DailyTalkRefreshDaySid, day, sync);
        return true;
    }

    internal static bool Eligible(PlayerAttributes attrs, HouseDailyTalkExcel row, ISet<uint> ownedGirls,
        ISet<ulong> ownedItems, DateTimeOffset now, bool allLevelsCompleted = false)
    {
        if (row.GirlId is < 1 or > 50 || row.StoryId is < 1 or > 45 || string.IsNullOrWhiteSpace(row.StoryKey)
            || !ownedGirls.Contains(row.GirlId) || !LivesIn(attrs, row.GirlId))
            return false;
        if (HouseDailyTalkExcel.Numbers(row.Girls).Any(x => !ownedGirls.Contains(x))) return false;
        if (HouseDailyTalkExcel.Numbers(row.Furnitures).Any(x => !FurnitureShown(attrs, x))) return false;
        if (!allLevelsCompleted && HouseDailyTalkExcel.Numbers(row.PreLevel)
                .Any(x => attrs.GetValue(AttrIds.Quest.LevelPassGid, x) == 0)) return false;
        var previous = HouseDailyTalkExcel.Numbers(row.PreStory);
        if (previous.Length > 0 && (previous.Length != 2 || !Read(attrs, previous[0], previous[1]))) return false;
        var love = HouseDailyTalkExcel.Numbers(row.CheckLoveLevel);
        if (love.Length > 0) return false; // No populated examples: do not guess this field's layout.
        var item = HouseDailyTalkExcel.Numbers(row.NeedItem);
        if (item.Length > 0 && (item.Length != 4 || !ownedItems.Contains(GameResourceTemplateId.FromGdpl(item))))
            return false;
        return InTime(row.BeginTime, now, begin: true) && InTime(row.EndTime, now, begin: false);
    }

    private static bool InTime(JToken? token, DateTimeOffset now, bool begin)
    {
        if (string.IsNullOrWhiteSpace(token?.ToString())) return true;
        if (!DateTime.TryParse(token.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            return false;
        var boundary = new DateTimeOffset(DateTime.SpecifyKind(time, DateTimeKind.Unspecified), TimeSpan.FromHours(8));
        return begin ? now >= boundary : now < boundary;
    }

    internal static bool LivesIn(PlayerAttributes attrs, uint girlId) =>
        attrs.GetValue(Gid, girlId * 50 + 1) is > 0 and < 100;

    internal static bool FurnitureShown(PlayerAttributes attrs, uint id)
    {
        if (!GameData.HouseSupportFurnitureData.TryGetValue(id, out var furniture)
            || furniture.AreaId is < 1 or > 50 || furniture.Index > 99) return false;
        var area = furniture.AreaId;
        var index = furniture.Index;
        if (((attrs.GetValue(Gid, area * 50 + 10 + index / 10) >> (int)(index % 10 * 3)) & 7) == 0
            || Folded(index)) return false;
        foreach (var group in GameData.HouseFurniturePosData.Values.Where(x => x.AreaId == area))
        {
            var ids = HouseDailyTalkExcel.Numbers(group.FurnitureTmpId);
            var position = Array.IndexOf(ids, id);
            if (position < 0 || group.GroupId is < 1 or > 10) continue;
            var selected = (attrs.GetValue(Gid, area * 50 + 20) >> (int)((group.GroupId - 1) * 3)) & 7;
            if (selected == position + 1) return true;
            return ids.Any(other => GameData.HouseSupportFurnitureData.TryGetValue(other, out var info)
                && Folded(info.Index));
        }
        return true;

        bool Folded(uint offset) => offset <= 30 && (attrs.GetValue(Gid, area * 50 + 23) & (1u << (int)offset)) != 0;
    }

    internal static (uint Sid, int Shift) ReadSlot(uint girlId, uint storyId)
    {
        var offset = storyId <= 15 ? 5u : storyId <= 30 ? 21u : 22u;
        return (girlId * 50 + offset, (int)(((storyId - 1) % 15 + 1) * 2));
    }

    internal static bool Read(PlayerAttributes attrs, uint girlId, uint storyId)
    {
        if (girlId is < 1 or > 50 || storyId is < 1 or > 45) return false;
        var (sid, shift) = ReadSlot(girlId, storyId);
        return ((attrs.GetValue(Gid, sid) >> shift) & 3) == 1;
    }

    internal static void Consume(PlayerAttributes attrs, uint girlId, uint storyId, NtfSyncPlayer sync)
    {
        var (sid, shift) = ReadSlot(girlId, storyId);
        Set(attrs, sid, (attrs.GetValue(Gid, sid) & ~(3u << shift)) | (1u << shift), sync);
        Set(attrs, girlId * 50 + 3, 0, sync);
        Set(attrs, AttrIds.House.DailyTalkGirlSid, 0, sync);
    }

    private static void Set(PlayerAttributes attrs, uint sid, uint value, NtfSyncPlayer? sync)
    {
        if (attrs.GetValue(Gid, sid) == value) return;
        attrs.Set(Gid, sid, value);
        if (sync != null) attrs.SyncTo(sync, Gid, sid, value);
    }
}
