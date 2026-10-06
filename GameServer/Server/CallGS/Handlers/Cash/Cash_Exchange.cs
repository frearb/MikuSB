using System.Text.Json.Serialization;
using MikuSB.Database;
using MikuSB.GameServer.Game.Player;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Cash;

[CallGSApi("Cash_Exchange")]
public class Cash_Exchange : CallGSHandler<CashExchangeParam>
{
    protected override Task<CallGSResult> HandleAsync(CallGSContext context, CashExchangeParam req)
    {
        var result = CashExchangeService.Exchange(context.Player, req, DateTimeOffset.UtcNow);
        DatabaseHelper.SaveDatabaseType(context.Player.InventoryManager.InventoryData);
        DatabaseHelper.SaveDatabaseType(context.Player.Data);
        return Task.FromResult(result);
    }
}

public sealed class CashExchangeParam
{
    // Preserve the spelling used by the client protocol.
    [JsonPropertyName("nExhcangeCash")] public uint Source { get; set; }
    [JsonPropertyName("nExhcangTarget")] public uint Target { get; set; }
    [JsonPropertyName("nCost")] public uint Cost { get; set; }
}
