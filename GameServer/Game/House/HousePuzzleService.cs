using System.Text.Json;
using MikuSB.Data;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;

namespace MikuSB.GameServer.Game.House;

internal static class HousePuzzleService
{
    private const uint Gid = AttrIds.House.Gid;
    private static readonly PuzzleConfig Config = LoadConfig();

    // Monday 04:00 UTC+8. Persist both the reset and grant markers so reconnects
    // cannot refill a consumed attempt, even if the client resends an unread flag.
    internal static uint Week(DateTimeOffset now)
    {
        var date = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(8)).AddHours(-4).DateTime);
        return (uint)(date.DayNumber - ((int)date.DayOfWeek + 6) % 7);
    }

    internal static void EnsureAvailable(PlayerAttributes attrs, DateTimeOffset now)
    {
        var week = Week(now);
        var previous = attrs.GetValue(Gid, AttrIds.House.PuzzleWeekSid);
        if (previous != week)
        {
            if (previous != 0) attrs.Set(Gid, AttrIds.House.HasOpenedPuzzleThisWeekSid, 0);
            attrs.Set(Gid, AttrIds.House.PuzzleWeekSid, week);
        }
        if (attrs.GetValue(Gid, AttrIds.House.PuzzleMapIdSid) != 0) return;
        if (attrs.GetValue(Gid, AttrIds.House.PuzzleRefreshTimesSid) == 0 &&
            attrs.GetValue(Gid, AttrIds.House.HasOpenedPuzzleThisWeekSid) == 0 &&
            attrs.GetValue(Gid, AttrIds.House.PuzzleAutoGrantWeekSid) != week)
        {
            attrs.Set(Gid, AttrIds.House.PuzzleRefreshTimesSid, 1);
            attrs.Set(Gid, AttrIds.House.PuzzleAutoGrantWeekSid, week);
            // Supply the configured starter pieces for this free attempt.
            foreach (var clip in Config.Clips.Where(x => x.DefaultNum > 0))
            {
                var sid = AttrIds.House.PuzzleClipCountStartSid + clip.ClipId;
                var packed = attrs.GetValue(Gid, sid);
                var missing = Math.Max(0, clip.DefaultNum - ClipCount(packed));
                attrs.Set(Gid, sid, packed + (uint)missing);
            }
        }
        if (attrs.GetValue(Gid, AttrIds.House.PuzzleRefreshTimesSid) > 0)
            CreateMap(attrs, null);
    }

    internal static bool CreateMap(PlayerAttributes attrs, NtfSyncPlayer? sync)
    {
        var times = attrs.GetValue(Gid, AttrIds.House.PuzzleRefreshTimesSid);
        if (times == 0) return false;
        // Use the first 6x4 board for the minimum flow; all saved boards can still
        // be validated and settled using the original client configuration.
        Set(attrs, AttrIds.House.PuzzleRefreshTimesSid, times - 1, sync);
        Set(attrs, AttrIds.House.PuzzleMapIdSid, 1, sync);
        Set(attrs, AttrIds.House.HouseInfoStartSid + 64, 1, sync);
        SetState(attrs, [], sync);
        return true;
    }

    internal static bool Save(PlayerAttributes attrs, PuzzlePlacement[] state, NtfSyncPlayer sync)
    {
        if (!Validate(attrs, state, out _, out _)) return false;
        SetState(attrs, state, sync);
        Set(attrs, AttrIds.House.HasOpenedPuzzleThisWeekSid, 1, sync);
        return true;
    }

    internal static PuzzlePlacement[]? ReadState(string? json)
    {
        try { return JsonSerializer.Deserialize<PuzzlePlacement[]>(json ?? "[]"); }
        catch (JsonException) { return null; }
    }

    internal static bool Complete(PlayerAttributes attrs, PuzzlePlacement[] state, NtfSyncPlayer sync,
        out List<IReadOnlyList<uint>> rewards)
    {
        rewards = [];
        if (!Validate(attrs, state, out var covered, out var map) || covered == 0) return false;
        var total = Board(map!).Count;
        foreach (var row in map!.PercentReward)
        {
            var scaled = row.ToArray();
            scaled[4] = (uint)((ulong)row[4] * (uint)covered / (uint)total);
            rewards.Add(scaled);
        }
        if (covered == total) rewards.AddRange(map.CompleteReward);
        foreach (var placement in state)
            rewards.AddRange(Config.Clips.Single(x => x.ClipId == placement.ClipId && x.RotId == placement.RotId).SpReward);

        foreach (var group in state.GroupBy(x => x.ClipId))
        {
            var sid = AttrIds.House.PuzzleClipCountStartSid + group.Key;
            var packed = attrs.GetValue(Gid, sid);
            var drop = packed & 0x7fffu;
            var friend = (packed >> 15) & 0xffffu;
            var usedDrop = Math.Min(drop, (uint)group.Count());
            drop -= usedDrop;
            friend -= (uint)group.Count() - usedDrop;
            Set(attrs, sid, (packed & 0x80000000u) | drop | (friend << 15), sync);
        }
        // A current map already represents one reserved attempt. Clearing it
        // consumes that attempt; refresh times were spent when the map was made.
        Set(attrs, AttrIds.House.PuzzleMapIdSid, 0, sync);
        SetState(attrs, [], sync);
        Set(attrs, AttrIds.House.HasOpenedPuzzleThisWeekSid, 1, sync);
        return true;
    }

    internal static bool Validate(PlayerAttributes attrs, PuzzlePlacement[] state, out int covered, out PuzzleMap? map)
    {
        covered = 0;
        map = Config.Maps.FirstOrDefault(x => x.MapId == attrs.GetValue(Gid, AttrIds.House.PuzzleMapIdSid));
        if (map == null || state.Length > Board(map).Count || state.Any(x => x == null || x.Pos < 0)) return false;
        foreach (var group in state.GroupBy(x => x.ClipId))
            if (group.Count() > ClipCount(attrs.GetValue(Gid, AttrIds.House.PuzzleClipCountStartSid + group.Key))) return false;
        var available = Board(map);
        foreach (var placement in state)
        {
            var clip = Config.Clips.FirstOrDefault(x => x.ClipId == placement.ClipId && x.RotId == placement.RotId);
            if (clip == null) return false;
            var startX = placement.Pos / 10 + clip.StartOffset[0];
            var startY = placement.Pos % 10 + clip.StartOffset[1];
            for (var y = 0; y < clip.Cells.Length; y++)
                for (var x = 0; x < clip.Cells[y].Length; x++)
                {
                    var px = startX + x;
                    var py = startY + y;
                    // The client checks bounds for the whole piece rectangle,
                    // but only occupied cells must avoid holes and other pieces.
                    if (px < 0 || py < 0 || px > map.MapSize[0] || py > map.MapSize[1]) return false;
                    if (clip.Cells[y][x] == 1)
                    {
                        if (!available.Remove((px, py))) return false;
                        covered++;
                    }
                }
        }
        return true;
    }

    private static int ClipCount(uint packed) => (int)((packed & 0x7fffu) + ((packed >> 15) & 0xffffu));

    private static HashSet<(int X, int Y)> Board(PuzzleMap map)
    {
        var cells = new HashSet<(int, int)>();
        for (var y = 0; y <= map.MapSize[1]; y++)
            for (var x = 0; x <= map.MapSize[0]; x++)
                if (y >= map.MapInfo.Length || !map.MapInfo[y].Contains(x)) cells.Add((x, y));
        return cells;
    }

    internal static void Set(PlayerAttributes attrs, uint sid, uint value, NtfSyncPlayer? sync)
    {
        attrs.Set(Gid, sid, value);
        if (sync != null) attrs.SyncTo(sync, Gid, sid, value);
    }

    private static void SetState(PlayerAttributes attrs, PuzzlePlacement[] state, NtfSyncPlayer? sync)
    {
        var attr = attrs.SetString(Gid, AttrIds.House.PuzzleMapStateSid, JsonSerializer.Serialize(state));
        if (sync != null) attrs.SyncTo(sync, attr);
    }

    private static PuzzleConfig LoadConfig()
    {
        // Exported read-only from Client_Settings/house/puzzle. The existing
        // converted clips.json flattens nested shapes, so it cannot validate cells.
        using var stream = typeof(HousePuzzleService).Assembly.GetManifestResourceStream("MikuSB.HousePuzzle.json")
            ?? throw new InvalidOperationException("House puzzle configuration is missing.");
        return JsonSerializer.Deserialize<PuzzleConfig>(stream)!;
    }

    private sealed class PuzzleConfig
    {
        public PuzzleMap[] Maps { get; set; } = [];
        public PuzzleClip[] Clips { get; set; } = [];
    }
    internal sealed class PuzzleMap
    {
        public uint MapId { get; set; }
        public int[] MapSize { get; set; } = [];
        public int[][] MapInfo { get; set; } = [];
        public uint[][] PercentReward { get; set; } = [];
        public uint[][] CompleteReward { get; set; } = [];
    }
    private sealed class PuzzleClip
    {
        public uint ClipId { get; set; }
        public uint RotId { get; set; }
        public int[] StartOffset { get; set; } = [];
        public int[][] Cells { get; set; } = [];
        public int DefaultNum { get; set; }
        public uint[][] SpReward { get; set; } = [];
    }
}

internal sealed class PuzzlePlacement
{
    public uint ClipId { get; set; }
    public uint RotId { get; set; }
    public int Pos { get; set; }
}
