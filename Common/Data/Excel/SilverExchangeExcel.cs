namespace MikuSB.Data.Excel;

[ResourceEntity("cash/silver_exchange.json")]
public class SilverExchangeExcel : ExcelResource
{
    public uint Times { get; set; }
    public uint Cost { get; set; }
    public uint Silver { get; set; }

    public override uint GetId() => Times;
    public override void Loaded() => GameData.SilverExchangeData[Times] = this;
}
