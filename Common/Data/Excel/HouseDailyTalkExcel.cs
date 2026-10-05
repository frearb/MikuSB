using Newtonsoft.Json.Linq;

namespace MikuSB.Data.Excel;

[ResourceEntity("house/daily_talk.json")]
public class HouseDailyTalkExcel : ExcelResource
{
    public uint GirlId { get; set; }
    public uint StoryId { get; set; }
    public string StoryKey { get; set; } = "";
    public JToken? CheckIn { get; set; }
    public JToken? Girls { get; set; }
    public JToken? Furnitures { get; set; }
    public JToken? PreLevel { get; set; }
    public JToken? PreStory { get; set; }
    public JToken? CheckLoveLevel { get; set; }
    public JToken? NeedItem { get; set; }
    public JToken? BeginTime { get; set; }
    public JToken? EndTime { get; set; }

    public override uint GetId() => GirlId * 100 + StoryId;
    public override void Loaded() => GameData.HouseDailyTalkData[GetId()] = this;

    public static uint Number(JToken? value) => uint.TryParse(value?.ToString(), out var number) ? number : 0;
    public static uint[] Numbers(JToken? value)
    {
        if (value is JArray array) return array.Select(Number).ToArray();
        var number = Number(value);
        return number > 0 ? [number] : [];
    }
}

[ResourceEntity("house/support_furniture.json")]
public class HouseSupportFurnitureExcel : ExcelResource
{
    public uint AreaId { get; set; }
    public uint Index { get; set; }
    public uint FurnitureTmpId { get; set; }
    public override uint GetId() => FurnitureTmpId;
    public override void Loaded() => GameData.HouseSupportFurnitureData[GetId()] = this;
}
