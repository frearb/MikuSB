using MikuSB.Data;
using MikuSB.Data.Excel;
using System.Text.Json;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Gacha;

internal static class GachaRotation
{
    private static readonly TimeZoneInfo ChinaTime = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai");
    private static readonly IReadOnlyDictionary<uint, int> DaysByPoolId = LoadDays();

    public static DateOnly Today() => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ChinaTime));

    public static bool IsOpen(GachaExcel gacha, DateOnly date)
    {
        if (gacha.Type is 4 or 6
            || !GameData.GachaProbabilityData.ContainsKey(gacha.Probability)
            || !(gacha.Pool?.Any(name => GameData.GachaPoolData.TryGetValue(name, out var items)
                && items.Any(item => item.GDPL.Count >= 4)) ?? false))
            return false;

        if (gacha.ID is 1 or 2 or 3)
            return true;

        var weekday = ((int)date.DayOfWeek + 6) % 7; // Monday is day zero.
        return DaysByPoolId.TryGetValue(gacha.ID, out var day) && day == weekday;
    }

    private static IReadOnlyDictionary<uint, int> LoadDays()
    {
        using var stream = typeof(GachaRotation).Assembly.GetManifestResourceStream("MikuSB.GachaRotation.json")
            ?? throw new InvalidOperationException("Embedded gacha rotation is missing.");
        var entries = JsonSerializer.Deserialize<List<RotationEntry>>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        var days = new Dictionary<uint, int>();
        foreach (var entry in entries)
        {
            if (entry.Day is < 0 or > 6)
                throw new InvalidDataException($"Invalid rotation day for {entry.Key}.");
            foreach (var id in entry.RoleIds.Concat(entry.WeaponIds))
            {
                if (!days.TryAdd(id, entry.Day))
                    throw new InvalidDataException($"Gacha pool {id} is in multiple rotation groups.");
            }
        }
        return days;
    }

    private sealed class RotationEntry
    {
        public string Key { get; set; } = "";
        public int Day { get; set; }
        public List<uint> RoleIds { get; set; } = [];
        public List<uint> WeaponIds { get; set; } = [];
    }
}
