using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database.Inventory;
using MikuSB.Proto;
using Newtonsoft.Json.Linq;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Gacha;

internal static class GachaCost
{
    internal static bool TrySelect(GachaExcel config, int drawCount, InventoryData inventory,
        out List<(uint UniqueId, uint Count)>? selected)
    {
        selected = null;
        var costs = drawCount == 1 ? config.CastOne : config.CastTen;
        if (costs is not JArray options || options.Count == 0 || options[0] is not JArray first
            || !TryReadCost(first, out _, out var firstCount))
            return false;

        // The client spends the special ticket first, then the last affordable normal option.
        if (config.CastSpecial is JArray special && special.Count >= 4
            && TryReadTemplate(special, out var specialTemplate)
            && TryAllocate(inventory, specialTemplate, firstCount, out selected))
            return true;

        foreach (var option in options.OfType<JArray>())
        {
            if (!TryReadCost(option, out var template, out var count))
                return false;
            if (TryAllocate(inventory, template, count, out var allocation))
                selected = allocation;
        }

        return selected != null;
    }

    internal static void Consume(InventoryData inventory, List<(uint UniqueId, uint Count)> selected,
        ICollection<Item> syncItems)
    {
        foreach (var (uniqueId, count) in selected)
        {
            var item = inventory.Items[uniqueId];
            item.ItemCount -= count;
            if (item.ItemCount == 0)
                inventory.Items.Remove(uniqueId);
            syncItems.Add(item.ToProto());
        }
    }

    private static bool TryAllocate(InventoryData inventory, ulong template, uint count,
        out List<(uint UniqueId, uint Count)>? allocation)
    {
        allocation = [];
        var remaining = count;
        foreach (var item in inventory.Items.Values.Where(item => item.TemplateId == template))
        {
            var take = Math.Min(remaining, item.ItemCount);
            if (take == 0) continue;
            allocation.Add((item.UniqueId, take));
            remaining -= take;
            if (remaining == 0) return true;
        }

        allocation = null;
        return false;
    }

    private static bool TryReadCost(JArray values, out ulong template, out uint count)
    {
        template = 0;
        count = 0;
        return values.Count >= 5 && TryReadTemplate(values, out template)
            && uint.TryParse(values[4]?.ToString(), out count) && count > 0;
    }

    private static bool TryReadTemplate(JArray values, out ulong template)
    {
        template = 0;
        if (values.Count < 4 || !uint.TryParse(values[0]?.ToString(), out var genre)
            || !uint.TryParse(values[1]?.ToString(), out var detail)
            || !uint.TryParse(values[2]?.ToString(), out var particular)
            || !uint.TryParse(values[3]?.ToString(), out var level))
            return false;
        template = GameResourceTemplateId.FromGdpl(genre, detail, particular, level);
        return true;
    }
}
