using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database.Inventory;
using MikuSB.GameServer.Server.CallGS.Handlers.Gacha;
using MikuSB.GameServer.Server.CallGS.Handlers.Items;
using MikuSB.Proto;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

var standard = GameResourceTemplateId.FromGdpl(5, 21, 1, 5);
var alternative = GameResourceTemplateId.FromGdpl(5, 21, 2, 5);
var special = GameResourceTemplateId.FromGdpl(5, 21, 4, 5);
var config = new GachaExcel
{
    CastOne = JArray.Parse("[[5,21,1,5,1],[5,21,2,5,1]]"),
    CastTen = JArray.Parse("[[5,21,1,5,10],[5,21,2,5,10]]"),
    CastSpecial = JArray.Parse("[5,21,4,5,1]")
};

var inventory = new InventoryData();
inventory.Items[1] = new BaseGameItemInfo { UniqueId = 1, TemplateId = standard, ItemCount = 9 };
Assert(!GachaCost.TrySelect(config, 10, inventory, out _), "Ten draws must fail with fewer than ten tickets");
Assert(inventory.Items[1].ItemCount == 9, "Failed draw must not consume tickets");

inventory.Items[2] = new BaseGameItemInfo { UniqueId = 2, TemplateId = standard, ItemCount = 1 };
Assert(GachaCost.TrySelect(config, 10, inventory, out var tenCost), "Ten draws should combine matching stacks");
var sync = new List<Item>();
GachaCost.Consume(inventory, tenCost!, sync);
Assert(inventory.Items.Count == 0 && sync.Count == 2 && sync.All(x => x.Count == 0),
    "Ten draws must remove both spent stacks and sync zero counts");

inventory.Items[3] = new BaseGameItemInfo { UniqueId = 3, TemplateId = standard, ItemCount = 1 };
inventory.Items[4] = new BaseGameItemInfo { UniqueId = 4, TemplateId = alternative, ItemCount = 1 };
Assert(GachaCost.TrySelect(config, 1, inventory, out var oneCost), "One draw should accept normal tickets");
Assert(oneCost!.Single().UniqueId == 4, "Client uses the last affordable normal option");
GachaCost.Consume(inventory, oneCost!, sync);
Assert(inventory.Items.ContainsKey(3) && !inventory.Items.ContainsKey(4), "Only the selected ticket is spent");

inventory.Items[5] = new BaseGameItemInfo { UniqueId = 5, TemplateId = special, ItemCount = 1 };
Assert(GachaCost.TrySelect(config, 1, inventory, out var specialCost), "Special ticket should be accepted");
Assert(specialCost!.Single().UniqueId == 5, "Special ticket must be preferred");
GachaCost.Consume(inventory, specialCost!, sync);
Assert(inventory.Items.ContainsKey(3) && !inventory.Items.ContainsKey(5), "Special draw must consume special ticket");

config.CastSpecial = JValue.CreateString("");
config.CastTen = JArray.Parse("[[5,21,1,5,8]]");
inventory.Items[3].ItemCount = 8;
Assert(GachaCost.TrySelect(config, 10, inventory, out var discountedCost), "Discounted ten draw should use configured cost");
Assert(discountedCost!.Single().Count == 8, "Discounted ten draw must spend eight tickets");

var permanentConfig = new GachaExcel
{
    CastOne = JArray.Parse("[[5,21,1,4,1]]"),
    CastTen = JArray.Parse("[[5,21,1,4,10]]"),
    CastSpecial = JValue.CreateString("")
};
Assert(!GachaCost.TrySelect(permanentConfig, 1, inventory, out _),
    "Limited tickets must not pay for a permanent draw");
inventory.Items[6] = new BaseGameItemInfo
{
    UniqueId = 6,
    TemplateId = GameResourceTemplateId.FromGdpl(5, 21, 1, 4),
    ItemCount = 1
};
Assert(GachaCost.TrySelect(permanentConfig, 1, inventory, out var permanentCost),
    "Permanent draw should accept its own ticket");
GachaCost.Consume(inventory, permanentCost!, sync);
Assert(!inventory.Items.ContainsKey(6), "Permanent draw must spend its ticket");

var exchangePath = Path.GetFullPath("MikuSB/bin/Debug/net10.0/Resources/item/exchange.json");
var exchanges = JsonConvert.DeserializeObject<List<ItemExchangeExcel>>(File.ReadAllText(exchangePath))!;
foreach (var exchange in exchanges) exchange.Loaded();
Assert(exchanges.Count == 4 && GameData.ItemExchangeData.Count == 4,
    "All four gacha ticket exchange rules must load");
Assert(exchanges.All(x => x.Cash.SequenceEqual([2u, 160u]) && x.Item.Count == 0),
    "Each ticket should cost 160 data gold");
var purchase = System.Text.Json.JsonSerializer.Deserialize<ItemExchangeParam>("{\"tbGDPLN\":[5,21,1,5,6]}")!;
Assert(purchase.TbGDPLN!.SequenceEqual([5u, 21u, 1u, 5u, 6u]),
    "The handler must accept the request shape captured in Server.log");

Console.WriteLine("Gacha cost checks passed.");

static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
