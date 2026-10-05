using MikuSB.Data.Excel;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Gacha;

public static class GachaProbabilityCorrection
{
    public static int[] GetWeights(GachaProbabilityExcel probability, bool tenGuarantee)
    {
        var original = probability.Weights;
        // Current limited pools use rarity 6 for orange. Permanent pools use
        // rarity 5, and ten-draw guarantee tables retain their JSON weights.
        if (tenGuarantee || probability.Rarity6 == 0)
            return original;

        if (original[0] != 0 || original[1] != 0 || original.Any(weight => weight < 0))
            throw new InvalidOperationException($"Unsupported gacha rarity weights in probability {probability.ID}.");

        var total = original.Sum(weight => (long)weight);
        if (total is <= 0 or > int.MaxValue)
            throw new InvalidOperationException($"Invalid gacha probability total in probability {probability.ID}.");

        var originalOrange = (long)probability.Rarity5 + probability.Rarity6;
        var orange = originalOrange * 100;
        var purple = (long)probability.Rarity4 * 10;
        if (orange + purple > total)
            throw new InvalidOperationException($"Adjusted gacha weights exceed the total in probability {probability.ID}.");

        var blue = total - orange - purple;
        var orange5 = originalOrange == 0 ? 0 : orange * probability.Rarity5 / originalOrange;
        var orange6 = orange - orange5;

        return [0, 0, (int)blue, (int)purple, (int)orange5, (int)orange6];
    }
}
