using MikuSB.Data;
using MikuSB.GameServer.Game.House;
using MikuSB.Proto;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.CallGS.Handlers.House;

[HouseFunc("DoDailyTalk")]
public class DoDailyTalk : IHouseFuncHandler
{
    public Task<CallGSResult> Handle(CallGSContext context, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return Task.FromResult(CallGSResult.NoResponse());
        var girlId = HouseJson.NumField(root, "GirlId");
        var attrs = context.Player.Attributes;
        var sync = new NtfSyncPlayer();
        HouseDailyEventService.Refresh(context.Player, sync);
        var review = root["Review"] is JsonValue flag && flag.TryGetValue<bool>(out var value) && value;
        var storyId = review ? (uint)Math.Max(0, HouseJson.NumField(root, "EventId"))
            : girlId is >= 1 and <= 50 ? attrs.GetValue(AttrIds.House.Gid, (uint)girlId * 50 + 3) : 0;
        if (girlId is < 1 or > 50 || storyId == 0 || !HouseDailyEventService.LivesIn(attrs, (uint)girlId)
            || (review && !HouseDailyEventService.Read(attrs, (uint)girlId, storyId))
            || !GameData.HouseDailyTalkData.ContainsKey((uint)girlId * 100 + storyId))
        {
            // Keep the callback name so HouseMessageHandle closes the connection overlay.
            return Task.FromResult(CallGSResult.Ok(new JsonObject
            {
                ["FuncName"] = "DoDailyTalk", ["GirlId"] = girlId, ["EventId"] = 0,
                ["bSuccess"] = false, ["nResult"] = 1
            }, sync));
        }

        if (!review) HouseDailyEventService.Consume(attrs, (uint)girlId, storyId, sync);
        return Task.FromResult(CallGSResult.Ok(HouseRequestScript.Success(new JsonObject
        {
            ["FuncName"] = "DoDailyTalk", ["GirlId"] = girlId, ["EventId"] = storyId,
            ["AddFavor"] = 0, ["Review"] = review
        }), sync));
    }
}
