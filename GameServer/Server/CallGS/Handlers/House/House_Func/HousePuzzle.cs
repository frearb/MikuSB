using MikuSB.Data;
using MikuSB.Database;
using MikuSB.GameServer.Game.House;
using MikuSB.Proto;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Server.CallGS.Handlers.House;

[HouseFunc("SavePuzzleMapState")]
[HouseFunc("FreeRefreshMap")]
[HouseFunc("GetPuzzleMapReward")]
public class HousePuzzle : IHouseFuncHandler
{
    public async Task<CallGSResult> Handle(CallGSContext context, string param)
    {
        var root = HouseJson.ParseObject(param);
        if (root == null) return CallGSResult.NoResponse();
        var func = root["FuncName"]?.GetValue<string>();
        var player = context.Player;
        var attrs = player.Attributes;
        var sync = new NtfSyncPlayer();
        var rewards = new JsonArray();
        var success = false;
        switch (func)
        {
            case "FreeRefreshMap":
                success = HousePuzzleService.CreateMap(attrs, sync);
                if (success) HousePuzzleService.Set(attrs, AttrIds.House.HasOpenedPuzzleThisWeekSid, 1, sync);
                break;
            case "SavePuzzleMapState":
            case "GetPuzzleMapReward":
                var state = HousePuzzleService.ReadState(root["MapState"]?.ToJsonString() ??
                    (func == "GetPuzzleMapReward" ? attrs.GetStringValue(AttrIds.House.Gid, AttrIds.House.PuzzleMapStateSid) : "null"));
                if (state == null) break;
                if (func == "SavePuzzleMapState") success = HousePuzzleService.Save(attrs, state, sync);
                else if (HousePuzzleService.Complete(attrs, state, sync, out var rows))
                {
                    rewards = await player.RewardManager.GrantConfiguredRewardsAsync(rows, sync);
                    DatabaseHelper.SaveDatabaseType(player.InventoryManager.InventoryData);
                    success = true;
                }
                break;
        }
        if (!success)
            return CallGSResult.Ok(new JsonObject { ["FuncName"] = func, ["bSuccess"] = false,
                ["nResult"] = 1, ["sErr"] = "error.BadParam" });

        DatabaseHelper.SaveDatabaseType(player.Data);
        await player.OnHeartBeat();
        return CallGSResult.Ok(HouseRequestScript.Success(new JsonObject
        {
            // Client request and callback names intentionally differ.
            ["FuncName"] = func == "GetPuzzleMapReward" ? "GetPuzzleRewardSuccess" :
                func == "SavePuzzleMapState" ? func : func + "Success",
            ["tbRewards"] = rewards,
            ["MapId"] = attrs.GetValue(AttrIds.House.Gid, AttrIds.House.PuzzleMapIdSid)
        }), sync);
    }
}
