using MikuSB.Data;
using System.Text.Json;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Gacha;

[CallGSApi("Gacha_GetOpenTime")]
public class Gacha_GetOpenTime : CallGSHandler
{
    // The client replaces every pool's local time range with this response.
    // Keep only pools that the server can actually draw from.
    private static readonly long[] OpenTime = [202101010000, 209912312359];
    private static readonly long[] ClosedTime = [202101010000, 202101010001];

    protected override Task<CallGSResult> HandleAsync(CallGSContext context, string param) =>
        Task.FromResult(CallGSResult.Ok(BuildResponse()));

    public static string BuildResponse()
    {
        // JSON object keys are strings. The client indexes tbOpenTime with numeric
        // pool IDs, so send a Lua-style, one-based array instead.
        var maxId = GameData.GachaData.Keys.DefaultIfEmpty(0u).Max();
        var poolTimes = Enumerable.Repeat(ClosedTime, (int)maxId).ToArray();
        foreach (var gacha in GameData.GachaData.Values)
        {
            if (!GameData.GachaProbabilityData.ContainsKey(gacha.Probability)
                || !(gacha.Pool?.Any(name => GameData.GachaPoolData.TryGetValue(name, out var items)
                    && items.Any(item => item.GDPL.Count >= 4)) ?? false))
                continue;

            poolTimes[gacha.ID - 1] = OpenTime;
        }

        return JsonSerializer.Serialize(new { tbOpenTime = poolTimes });
    }
}
