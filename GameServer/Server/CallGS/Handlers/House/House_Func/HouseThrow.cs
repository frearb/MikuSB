using MikuSB.Proto;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.CallGS.Handlers.House;

[HouseFunc("GameEnterMainUI")]
public class ThrowGameEnterMainUI : IHouseFuncHandler
{
    public Task<CallGSResult> Handle(CallGSContext context, string param)
    {
        var rsp = new JsonObject
        {
            ["FuncName"] = "GameEnterMainUI",
            ["tblockGirlList"] = new JsonArray()
        };
        return Task.FromResult(CallGSResult.Ok(HouseRequestScript.Success(rsp)));
    }
}

[HouseFunc("ThrowGameTutorialFinish")]
public class ThrowGameTutorialFinish : IHouseFuncHandler
{
    private const uint HouseBeachInfoStart = 17000;
    private const uint ThrowGameTutorialOffset = 10;

    public async Task<CallGSResult> Handle(CallGSContext context, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return CallGSResult.NoResponse();
        var index = Math.Max(0, HouseJson.NumField(root, "Index"));
        var sync = new NtfSyncPlayer();
        await HouseAttr.SetAsync(context.Connection, HouseBeachInfoStart + ThrowGameTutorialOffset + (uint)index, 1, sync);
        return CallGSResult.Ok(HouseRequestScript.Synthesize(root), sync);
    }
}

[HouseFunc("ThrowGameEnter")]
public class ThrowGameEnter : IHouseFuncHandler
{
    private static readonly Random Random = new();

    public Task<CallGSResult> Handle(CallGSContext context, string param)
    {
        var rsp = new JsonObject
        {
            ["FuncName"] = "ThrowGameEnter",
            ["nSeed"] = Random.Next(1, 1_000_000_000)
        };
        return Task.FromResult(CallGSResult.Ok(HouseRequestScript.Success(rsp)));
    }
}

[HouseFunc("ThrowGameSettlement")]
public class ThrowGameSettlement : IHouseFuncHandler
{
    private const uint HouseBeachInfoStart = 17000;
    private const uint ThrowGameChallengePointsOffset = 2;
    private const uint ThrowGameUnlockDrinkStartOffset = 50;
    private const int ThrowGameModeChallenge = 2;

    public async Task<CallGSResult> Handle(CallGSContext context, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return CallGSResult.NoResponse();
        var modeType = HouseJson.NumField(root, "nModeType");
        var score = Math.Max(0, HouseJson.NumField(root, "nScore"));
        var sync = new NtfSyncPlayer();
        if (root["tbUnlockDrink"] is JsonObject unlockDrink && unlockDrink["nDrinkIDs"] is JsonArray drinks)
        {
            foreach (var drinkNode in drinks)
            {
                var drinkId = HouseJson.ToInt(drinkNode);
                if (drinkId <= 0) continue;
                var sid = HouseBeachInfoStart + ThrowGameUnlockDrinkStartOffset + (uint)drinkId;
                var prev = HouseAttr.Read(context.Connection.Player!, sid);
                await HouseAttr.SetAsync(context.Connection, sid, prev + 1, sync);
            }
        }

        if (modeType == ThrowGameModeChallenge && score > 0)
        {
            var sid = HouseBeachInfoStart + ThrowGameChallengePointsOffset;
            var prev = HouseAttr.Read(context.Connection.Player!, sid);
            if ((uint)score > prev)
                await HouseAttr.SetAsync(context.Connection, sid, (uint)score, sync);
        }

        var rsp = new JsonObject
        {
            ["nAddExp"] = score,
            ["FuncName"] = "ThrowGameSettlement"
        };
        return CallGSResult.Ok(HouseRequestScript.Success(rsp), sync);
    }
}

[HouseFunc("ThrowGameGetLevelReward")]
public class ThrowGameGetLevelReward : IHouseFuncHandler
{
    public Task<CallGSResult> Handle(CallGSContext context, string param)
    {
        var rsp = new JsonObject { ["FuncName"] = "ThrowGameGetLevelReward" };
        return Task.FromResult(CallGSResult.Ok(HouseRequestScript.Success(rsp)));
    }
}

[HouseFunc("ThrowGameGetAchReward")]
public class ThrowGameGetAchReward : IHouseFuncHandler
{
    public Task<CallGSResult> Handle(CallGSContext context, string param)
    {
        var rsp = new JsonObject { ["FuncName"] = "ThrowGameGetAchReward" };
        return Task.FromResult(CallGSResult.Ok(HouseRequestScript.Success(rsp)));
    }
}
