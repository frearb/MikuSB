using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Preview;

[CallGSApi("RandomShowGirl_SetMainBg")]
public class RandomShowGirl_SetMainBg : ICallGSHandler
{
    private const uint GroupId = 139;
    private const uint RandomMainSkinStartSid = 401;
    private const uint RandomMainSkinEndSid = 500;
    private const uint BgModeSid = 901;
    private const uint RandomCgStartSid = 1000;
    private const uint RandomCgEndSid = 2000;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<RandomShowGirlSetMainBgParam>(param);
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "RandomShowGirl_SetMainBg", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var player = connection.Player!;
        var sync = new NtfSyncPlayer();

        RandomShowGirlAttrSync.ClearRange(player, sync, GroupId, RandomMainSkinStartSid, RandomMainSkinEndSid);
        RandomShowGirlAttrSync.ClearRange(player, sync, GroupId, RandomCgStartSid, RandomCgEndSid);

        if (req.Is3D)
        {
            RandomShowGirlAttrSync.SetRange(player, sync, GroupId, RandomMainSkinStartSid, RandomMainSkinEndSid, req.Ids);
            RandomShowGirlAttrSync.SetAttr(player, sync, GroupId, BgModeSid, 0);
        }
        else
        {
            RandomShowGirlAttrSync.SetRange(player, sync, GroupId, RandomCgStartSid, RandomCgEndSid, req.Ids);
            RandomShowGirlAttrSync.SetAttr(player, sync, GroupId, BgModeSid, 1);
        }

        await CallGSRouter.SendScript(connection, "RandomShowGirl_SetMainBg", "{}", sync);
    }
}

internal sealed class RandomShowGirlSetMainBgParam
{
    [JsonPropertyName("b3D")]
    public bool Is3D { get; set; }

    [JsonPropertyName("tbId")]
    public List<uint> Ids { get; set; } = [];
}
