using MikuSB.Database;
using MikuSB.Database.Character;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Girl;

[CallGSApi("GirlSkin_RandomChange")]
public class GirlSkin_RandomChange : ICallGSHandler
{
    private const uint FashionGroupId = 58;
    private const uint RandomChangeRecordSid = 2;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<GirlSkinRandomChangeParam>(param);
        if (req == null || req.CardIds.Count == 0 || req.SkinIds.Count == 0)
        {
            await CallGSRouter.SendScript(connection, "GirlSkin_RandomChange", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var player = connection.Player!;
        var record = RandomChangeRecord.Read(player);
        var changedCards = new List<CharacterInfo>();

        var count = Math.Min(req.CardIds.Count, req.SkinIds.Count);
        for (var i = 0; i < count; i++)
        {
            var card = player.CharacterManager.GetCharacterByGUID(req.CardIds[i]);
            if (card == null)
                continue;

            var skinId = req.SkinIds[i];
            if (skinId == 0 || player.InventoryManager.GetSkinItem(skinId) == null)
                continue;

            record.TryAdd(card.Guid, card.SkinId);
            card.SkinId = skinId;
            changedCards.Add(card);
        }

        if (changedCards.Count == 0)
        {
            await CallGSRouter.SendScript(connection, "GirlSkin_RandomChange", "{\"sErr\":\"error.BadParam\"}");
            return;
        }

        var recordJson = record.ToJson();
        player.SetStrAttr(FashionGroupId, RandomChangeRecordSid, recordJson);
        DatabaseHelper.SaveDatabaseType(player.CharacterManager.CharacterData);
        DatabaseHelper.SaveDatabaseType(player.Data);

        var sync = new NtfSyncPlayer
        {
            Items = { changedCards.Select(x => x.ToProto()) }
        };
        sync.CustomStr[player.ToShiftedAttrKey(FashionGroupId, RandomChangeRecordSid)] = recordJson;

        await CallGSRouter.SendScript(connection, "GirlSkin_RandomChange", "{}", sync);
    }

    internal sealed class GirlSkinRandomChangeParam
    {
        [JsonPropertyName("tbCardId")]
        public List<uint> CardIds { get; set; } = [];

        [JsonPropertyName("tbId")]
        public List<uint> SkinIds { get; set; } = [];
    }

    internal sealed class RandomChangeRecord
    {
        [JsonPropertyName("tbCardId")]
        public List<uint> CardIds { get; set; } = [];

        [JsonPropertyName("tbId")]
        public List<uint> SkinIds { get; set; } = [];

        public static RandomChangeRecord Read(PlayerInstance player)
        {
            var raw = player.Data.StrAttrs.FirstOrDefault(x => x.Gid == FashionGroupId && x.Sid == RandomChangeRecordSid)?.Val;
            if (string.IsNullOrWhiteSpace(raw))
                return new RandomChangeRecord();

            try
            {
                return JsonSerializer.Deserialize<RandomChangeRecord>(raw) ?? new RandomChangeRecord();
            }
            catch
            {
                return new RandomChangeRecord();
            }
        }

        public void TryAdd(uint cardId, uint skinId)
        {
            if (CardIds.Contains(cardId))
                return;

            CardIds.Add(cardId);
            SkinIds.Add(skinId);
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}

[CallGSApi("GirlSkin_ResetRandomChange")]
public class GirlSkin_ResetRandomChange : ICallGSHandler
{
    private const uint FashionGroupId = 58;
    private const uint RandomChangeRecordSid = 2;

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var player = connection.Player!;
        var record = GirlSkin_RandomChange.RandomChangeRecord.Read(player);
        var changedCards = new List<CharacterInfo>();

        var count = Math.Min(record.CardIds.Count, record.SkinIds.Count);
        for (var i = 0; i < count; i++)
        {
            var card = player.CharacterManager.GetCharacterByGUID(record.CardIds[i]);
            if (card == null)
                continue;

            card.SkinId = record.SkinIds[i];
            changedCards.Add(card);
        }

        player.SetStrAttr(FashionGroupId, RandomChangeRecordSid, "");
        DatabaseHelper.SaveDatabaseType(player.CharacterManager.CharacterData);
        DatabaseHelper.SaveDatabaseType(player.Data);

        var sync = new NtfSyncPlayer
        {
            Items = { changedCards.Select(x => x.ToProto()) }
        };
        sync.CustomStr[player.ToShiftedAttrKey(FashionGroupId, RandomChangeRecordSid)] = "";

        await CallGSRouter.SendScript(connection, "GirlSkin_ResetRandomChange", "{}", sync);
    }
}
