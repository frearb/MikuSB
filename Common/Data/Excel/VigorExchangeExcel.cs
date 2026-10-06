namespace MikuSB.Data.Excel;

[ResourceEntity("cash/vigor_exchange.json")]
public class VigorExchangeExcel : ExcelResource
{
    public uint Times { get; set; }
    public uint Cost { get; set; }
    public uint Vigor { get; set; }

    public override uint GetId() => Times;
    public override void Loaded() => GameData.VigorExchangeData[Times] = this;
}
