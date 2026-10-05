using System.Text.Json;
using MikuSB.Data.Excel;
using MikuSB.GameServer.Server.CallGS.Handlers.Gacha;

var path = Path.GetFullPath("MikuSB/bin/Debug/net10.0/Resources/gacha/probability.json");
var probabilities = JsonSerializer.Deserialize<List<GachaProbabilityExcel>>(File.ReadAllText(path))!;
Assert(probabilities.Count == 8, "Expected all eight configured gacha probability tables");

foreach (var probability in probabilities)
{
    var weights = GachaProbabilityCorrection.GetWeights(probability, tenGuarantee: probability.Rarity3 == 0);
    Assert(weights.All(weight => weight >= 0), $"Negative adjusted weight in {probability.ID}");
    Assert(weights.Sum() == probability.Weights.Sum(), $"Adjusted total differs in {probability.ID}");
    Assert(weights[0] == 0 && weights[1] == 0, $"Unexpected lower rarity in {probability.ID}");
}

AssertWeights(10001, blue: 9130, purple: 800, orange5: 70, orange6: 0);
AssertWeights(10002, blue: 9130, purple: 800, orange5: 70, orange6: 0);
AssertWeights(10003, blue: 0, purple: 9930, orange5: 70, orange6: 0);
AssertWeights(10004, blue: 0, purple: 9930, orange5: 70, orange6: 0);
AssertWeights(10011, blue: 1000, purple: 8000, orange5: 0, orange6: 1000);
AssertWeights(10012, blue: 1000, purple: 8000, orange5: 0, orange6: 1000);
AssertWeights(10013, blue: 0, purple: 9990, orange5: 0, orange6: 10);
AssertWeights(10014, blue: 0, purple: 9990, orange5: 0, orange6: 10);
Assert(GachaProbabilityCorrection.GetWeights(probabilities.Single(value => value.ID == 10011), tenGuarantee: true)
    .SequenceEqual([0, 0, 9190, 800, 0, 10]), "Ten-draw guarantee must bypass limited-pool scaling");

Console.WriteLine("All gacha probability corrections passed.");

void AssertWeights(uint id, int blue, int purple, int orange5, int orange6)
{
    var probability = probabilities.Single(value => value.ID == id);
    var actual = GachaProbabilityCorrection.GetWeights(probability, tenGuarantee: probability.Rarity3 == 0);
    Assert(actual.SequenceEqual([0, 0, blue, purple, orange5, orange6]),
        $"Unexpected adjusted weights in probability {id}: [{string.Join(',', actual)}]");
}

static void Assert(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}
