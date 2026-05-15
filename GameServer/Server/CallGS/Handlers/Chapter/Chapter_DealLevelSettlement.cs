using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using MikuSB.GameServer.Server.CallGS.Handlers.Boss;
using MikuSB.Proto;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Chapter;

[CallGSApi("Chapter_DealLevelSettlement")]
public class Chapter_DealLevelSettlement : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<DealLevelSettlementParam>(param);
        var sync = default(NtfSyncPlayer);
        var response = new JsonObject
        {
            ["sCmd"] = req?.SCmd ?? "Chapter_LevelSettlement",
            ["tbParam"] = BuildSettlementPayload(connection, req?.SCmd, req?.TbParam, out sync)
        };

        if (sync != null)
        {
            await BossPvpLogicState.SendStrAttrsAsync(connection);
        }

        await CallGSRouter.SendScript(connection, "Chapter_DealLevelSettlement", response.ToJsonString(), sync!);
    }

    private static JsonNode BuildSettlementPayload(Connection connection, string? sCmd, JsonNode? tbParam, out NtfSyncPlayer? sync)
    {
        sync = null;
        if (string.Equals(sCmd, "BossPvpLogic_LevelSettlement", StringComparison.Ordinal) ||
            string.Equals(sCmd, "BossPvpLogic_LevelFail", StringComparison.Ordinal))
        {
            return BossPvpLogicState.HandleReconnectSettlement(connection.Player!, sCmd, tbParam, out sync);
        }

        if (string.Equals(sCmd, "Chapter_LevelSettlement", StringComparison.Ordinal))
        {
            return new JsonArray();
        }

        if (string.Equals(sCmd, "Chapter_NewPrologueSettlement", StringComparison.Ordinal))
        {
            var result = new JsonObject();
            if (tbParam is JsonObject source && source.TryGetPropertyValue("bWaitServer", out var bWaitServer))
            {
                result["bWaitServer"] = bWaitServer?.DeepClone();
            }
            result["tbShowAward"] = new JsonArray();
            return result;
        }

        return tbParam?.DeepClone() ?? new JsonObject();
    }
}

internal sealed class DealLevelSettlementParam
{
    [JsonPropertyName("sCmd")]
    public string? SCmd { get; set; }

    [JsonPropertyName("tbParam")]
    public JsonNode? TbParam { get; set; }
}
