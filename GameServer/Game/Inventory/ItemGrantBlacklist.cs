namespace MikuSB.GameServer.Game.Inventory;

public static class ItemGrantBlacklist
{
    // Items removed from UID 10001 after the four client inventory probes.
    // All 28 entries in the 5-27-1..28-1 series were removed together.
    private static readonly HashSet<(uint Genre, uint Detail, uint Particular, uint Level)> OtherEntries =
    [
        (5, 12, 1, 1),
        (5, 17, 1, 1),
        (5, 23, 1, 1),
        (5, 24, 1, 1),
        (5, 24, 3, 1),
        (5, 29, 20, 1),
        (5, 13, 2, 2),
        (4, 1, 6, 2),
        (5, 25, 2, 3),
        (4, 1, 6, 3),
        (5, 29, 20, 3),
        (5, 11, 1, 4),
        (5, 30, 2, 4),
        (5, 17, 1, 2),
        (5, 17, 1, 3)
    ];

    public static bool IsBlocked(uint genre, uint detail, uint particular, uint level) =>
        (genre == 5 && detail == 27 && particular is >= 1 and <= 28 && level == 1) ||
        OtherEntries.Contains((genre, detail, particular, level));
}
