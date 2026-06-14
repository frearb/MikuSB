using MikuSB.Database;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Misc;

[CallGSApi("PlayerSetting_SetBitthdayShow")]
public class PlayerSetting_SetBitthdayShow : ICallGSHandler
{
    private const int BirthdayShowAttr = 24;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<SetBitthdayShowParam>(param);
        if (req == null)
        {
            await CallGSRouter.SendScript(connection, "PlayerSetting_SetBitthdayShow", "{\"err\":\"error.BadParam\"}");
            return;
        }

        var player = connection.Player!;
        player.SetShowAttr(BirthdayShowAttr, req.Show ? 1u : 2u);
        DatabaseHelper.SaveDatabaseType(player.Data);

        var sync = new NtfSyncPlayer();
        sync.ShowAttrs.AddRange(player.Data.ShowAttrs);
        await CallGSRouter.SendScript(connection, "PlayerSetting_SetBitthdayShow", "null", sync);
    }
}

internal sealed class SetBitthdayShowParam
{
    [JsonPropertyName("bShow")]
    public bool Show { get; set; }
}
