using Newtonsoft.Json;

using Newtonsoft.Json.Linq;

namespace MikuSB.Data.Excel;

[ResourceEntity("item/templates/weapon_parts.json")]
public class WeaponPartsExcel : ExcelResource
{
    public uint Genre { get; set; }
    public uint Detail { get; set; }
    public uint Particular { get; set; }
    public uint Level { get; set; }
    [JsonProperty("Close")] public JToken? CloseRaw { get; set; }
    public uint Icon { get; set; }
    public int AppearID { get; set; }
    public string I18n { get; set; } = "";
    [JsonIgnore] public bool IsClosed => CloseRaw?.Type switch
    {
        JTokenType.Integer => CloseRaw.Value<int>() == 1,
        JTokenType.String => CloseRaw.Value<string>() == "1",
        _ => false
    };

    public override uint GetId()
    {
        return (uint)I18n.GetHashCode();
    }

    public override void Loaded()
    {
        GameData.WeaponPartsData.Add(GetId(), this);
    }
}
