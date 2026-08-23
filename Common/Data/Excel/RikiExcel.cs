using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MikuSB.Data.Excel;

[ResourceEntity("riki/Riki.json")]
public class RikiExcel : ExcelResource
{
    public uint Type { get; set; }
    public uint Id { get; set; }
    [JsonProperty("Condition")] public JToken? Condition { get; set; }
    public IReadOnlyList<uint> ItemCondition { get; private set; } = [];

    public override uint GetId() => Id;

    public override void Loaded()
    {
        if (Type is not (1 or 2 or 5) || Condition is not JArray condition ||
            condition.Count < 4 || condition.Any(x => x.Type != JTokenType.Integer))
            return;

        ItemCondition = condition.Select(x => x.Value<uint>()).ToArray();
        GameData.RikiData[Id] = this;
    }
}
