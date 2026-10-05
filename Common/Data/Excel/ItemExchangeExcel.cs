namespace MikuSB.Data.Excel;

[ResourceEntity("item/exchange.json")]
public class ItemExchangeExcel : ExcelResource
{
    public uint G { get; set; }
    public uint D { get; set; }
    public uint P { get; set; }
    public uint L { get; set; }
    public List<uint> Cash { get; set; } = [];
    public List<uint> Item { get; set; } = [];

    public override uint GetId() => (uint)GameResourceTemplateId.FromGdpl(G, D, P, L);
    public override void Loaded() => GameData.ItemExchangeData[GameResourceTemplateId.FromGdpl(G, D, P, L)] = this;
}
