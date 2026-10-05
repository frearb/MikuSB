namespace MikuSB.Data.Excel;

[ResourceEntity("house/FurniturePos.json")]
public class HouseFurniturePosExcel : ExcelResource
{
    public uint AreaId { get; set; }
    public uint GroupId { get; set; }
    public Newtonsoft.Json.Linq.JToken? FurnitureTmpId { get; set; }

    public override uint GetId()
    {
        return (AreaId << 16) | GroupId;
    }

    public override void Loaded()
    {
        GameData.HouseFurniturePosData.TryAdd(GetId(), this);
    }
}
