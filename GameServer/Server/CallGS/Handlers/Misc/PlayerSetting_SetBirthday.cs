using MikuSB.Database;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Misc;

[CallGSApi("PlayerSetting_SetBirthday")]
public class PlayerSetting_SetBirthday : ICallGSHandler
{
    private const int BirthdayMonthShowAttr = 22;
    private const int BirthdayDayShowAttr = 23;
    private const int BirthdayShowAttr = 24;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<SetBirthdayParam>(param);
        if (req == null || !IsValidBirthday(req.Month, req.Day))
        {
            await CallGSRouter.SendScript(connection, "PlayerSetting_SetBirthday", "{\"err\":\"error.BadParam\"}");
            return;
        }

        var player = connection.Player!;
        player.SetShowAttr(BirthdayMonthShowAttr, req.Month);
        player.SetShowAttr(BirthdayDayShowAttr, req.Day);

        if (player.Data.ShowAttrs.Count < BirthdayShowAttr || player.Data.ShowAttrs[BirthdayShowAttr - 1] == 0)
            player.SetShowAttr(BirthdayShowAttr, 2);

        DatabaseHelper.SaveDatabaseType(player.Data);

        var sync = new NtfSyncPlayer();
        sync.ShowAttrs.AddRange(player.Data.ShowAttrs);
        await CallGSRouter.SendScript(connection, "PlayerSetting_SetBirthday", "null", sync);
    }

    private static bool IsValidBirthday(uint month, uint day)
    {
        if (month is < 1 or > 12)
            return false;

        var daysInMonth = month == 2
            ? 29
            : month is 4 or 6 or 9 or 11 ? 30 : 31;

        return day >= 1 && day <= daysInMonth;
    }
}

internal sealed class SetBirthdayParam
{
    [JsonPropertyName("nMouth")]
    public uint Month { get; set; }

    [JsonPropertyName("nDay")]
    public uint Day { get; set; }
}
