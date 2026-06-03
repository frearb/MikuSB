using MikuSB.Database.Player;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Preview;

[CallGSApi("RandomShowGirl_Set")]
public class RandomShowGirl_Set : ICallGSHandler
{
    private const uint GroupId = 139;
    private const uint FormGroupId = 188;
    private const uint RandomGirlStartSid = 0;
    private const uint RandomGirlEndSid = 300;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<RandomShowGirlSetParam>(param);
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "RandomShowGirl_Set", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var player = connection.Player!;
        var sync = new NtfSyncPlayer();

        RandomShowGirlAttrSync.ClearRange(player, sync, GroupId, RandomGirlStartSid, RandomGirlEndSid);
        RandomShowGirlAttrSync.ClearRange(player, sync, FormGroupId, RandomGirlStartSid, RandomGirlEndSid);

        var count = Math.Min(req.ItemIds.Count, (int)(RandomGirlEndSid - RandomGirlStartSid + 1));

        for (var i = 0; i < count; i++)
        {
            var itemId = req.ItemIds[i];
            if (itemId == 0)
                continue;

            var sid = RandomGirlStartSid + (uint)i;
            RandomShowGirlAttrSync.SetAttr(player, sync, GroupId, sid, itemId);
            RandomShowGirlAttrSync.SetAttr(player, sync, FormGroupId, sid, i < req.Forms.Count ? req.Forms[i] : 0);
        }

        await CallGSRouter.SendScript(connection, "RandomShowGirl_Set", "{}", sync);
    }
}

internal sealed class RandomShowGirlSetParam
{
    [JsonPropertyName("tbItemId")]
    public List<uint> ItemIds { get; set; } = [];

    [JsonPropertyName("tbForm")]
    public List<uint> Forms { get; set; } = [];
}
