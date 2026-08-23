using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.CallGS.Handlers.House;

// PubGameEnter — returns nSeed for client-side game initialization.
[HouseFunc("PubGameEnter")]
public class PubGameEnter : IHouseFuncHandler
{
    private static readonly Random Random = new();

    public Task<CallGSResult> Handle(CallGSContext context, string param)
    {
        var rsp = new JsonObject
        {
            ["FuncName"] = "PubGameEnter",
            ["nSeed"] = Random.Next(1, 1_000_000_000),
            ["nModeType"] = 1,
            ["bIsGuide"] = false,
            ["bHasTry"] = false
        };
        return Task.FromResult(CallGSResult.Ok(rsp.ToJsonString()));
    }
}

[HouseFunc("PubGameMulExit")]
public class PubGameMulExit : IHouseFuncHandler
{
    public Task<CallGSResult> Handle(CallGSContext context, string param)
    {
        var rsp = new JsonObject { ["FuncName"] = "PubGameMulExit" };
        return Task.FromResult(CallGSResult.Ok(rsp.ToJsonString()));
    }
}

// PubGameSettlement — nAddExp=0 on private server.
[HouseFunc("PubGameSettlement")]
public class PubGameSettlement : IHouseFuncHandler
{
    public Task<CallGSResult> Handle(CallGSContext context, string param)
    {
        var rsp = new JsonObject { ["FuncName"] = "PubGameSettlement", ["nAddExp"] = 0 };
        return Task.FromResult(CallGSResult.Ok(rsp.ToJsonString()));
    }
}

[HouseFunc("PubGameGetReward")]
public class PubGameGetReward : IHouseFuncHandler
{
    public Task<CallGSResult> Handle(CallGSContext context, string param)
    {
        var rsp = new JsonObject { ["FuncName"] = "PubGameGetReward" };
        return Task.FromResult(CallGSResult.Ok(rsp.ToJsonString()));
    }
}

[HouseFunc("PubGameGetAchReward")]
public class PubGameGetAchReward : IHouseFuncHandler
{
    public Task<CallGSResult> Handle(CallGSContext context, string param)
    {
        var rsp = new JsonObject { ["FuncName"] = "PubGameGetAchReward" };
        return Task.FromResult(CallGSResult.Ok(rsp.ToJsonString()));
    }
}

[HouseFunc("PubGameAchievementFinish")]
public class PubGameAchievementFinish : IHouseFuncHandler
{
    public Task<CallGSResult> Handle(CallGSContext context, string param)
    {
        var rsp = new JsonObject { ["FuncName"] = "PubGameAchievementFinish" };
        return Task.FromResult(CallGSResult.Ok(rsp.ToJsonString()));
    }
}
