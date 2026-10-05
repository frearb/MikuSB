using MikuSB.Data;
using MikuSB.Database;
using MikuSB.GameServer.Game.Inventory;
using MikuSB.Proto;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Items;

[CallGSApi("Item_Exchange")]
public class Item_Exchange : CallGSHandler<ItemExchangeParam>
{
    protected override async Task<CallGSResult> HandleAsync(CallGSContext context, ItemExchangeParam req)
    {
        var row = req.TbGDPLN;
        if (row is not { Count: 5 } || row[4] == 0 || row[0] != 5 || row[1] != 21
            || row[2] is not (1 or 2) || row[3] is not (4 or 5))
            return CallGSResult.Error("error.BadParam");

        var templateId = GameResourceTemplateId.FromGdpl(row[0], row[1], row[2], row[3]);
        if (!GameData.ItemExchangeData.TryGetValue(templateId, out var exchange)
            || exchange.Cash.Count != 2 || exchange.Cash[1] == 0 || exchange.Item.Count != 0
            || ItemGrantBlacklist.IsBlocked(row[0], row[1], row[2], row[3]))
            return CallGSResult.Error("error.BadParam");

        var supplies = GameData.AllSuppliesData.FirstOrDefault(x =>
            x.Genre == row[0] && x.Detail == row[1] && x.Particular == row[2] && x.Level == row[3]);
        if (supplies == null || supplies.IsClosed)
            return CallGSResult.Error("error.BadParam");

        var player = context.Player;
        var inventory = player.InventoryManager.InventoryData;
        var owned = inventory.Items.Values.Where(x => x.TemplateId == templateId)
            .Aggregate(0UL, (total, item) => total + item.ItemCount);
        if ((ulong)row[4] + owned > 99999)
            return CallGSResult.Error("error.BadParam");

        var totalCost = (ulong)exchange.Cash[1] * row[4];
        if (totalCost > uint.MaxValue || player.GetCurrencyBalance(exchange.Cash[0]) < totalCost)
            return CallGSResult.Error("tip.cash_not_enough");

        var sync = new NtfSyncPlayer();
        if (!player.TrySpendCurrency(exchange.Cash[0], (uint)totalCost, sync))
            return CallGSResult.Error("tip.cash_not_enough");

        var ticket = await player.InventoryManager.AddSuppliesItem(supplies, row[4], sendPacket: false);
        if (ticket == null)
            throw new InvalidOperationException("Validated gacha ticket could not be granted.");
        sync.Items.Add(ticket.ToProto());

        DatabaseHelper.SaveDatabaseType(inventory);
        DatabaseHelper.SaveDatabaseType(player.Data);
        return CallGSResult.Ok("{}", sync);
    }
}

public sealed class ItemExchangeParam
{
    [JsonPropertyName("tbGDPLN")]
    public List<uint>? TbGDPLN { get; set; }
}
