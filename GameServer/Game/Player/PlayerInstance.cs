using Google.Protobuf;
using MikuSB.Data;
using MikuSB.Data.Excel;
using MikuSB.Database;
using MikuSB.Database.Account;
using MikuSB.Database.Inventory;
using MikuSB.Database.Player;
using MikuSB.Enums.Item;
using MikuSB.GameServer.Command;
using MikuSB.GameServer.Game.Character;
using MikuSB.GameServer.Game.Inventory;
using MikuSB.GameServer.Game.Lineup;
using MikuSB.GameServer.Game.Quest;
using MikuSB.GameServer.Game.Reward;
using MikuSB.GameServer.Game.Riki;
using MikuSB.GameServer.Game.Rogue3D;
using MikuSB.GameServer.Server;
using MikuSB.Proto;
using MikuSB.TcpSharp;
using MikuSB.Util;
using MikuSB.Util.Extensions;
using System.Text.Json.Nodes;

namespace MikuSB.GameServer.Game.Player;

public class PlayerInstance(PlayerGameData data)
{
    private static readonly bool EnableLoginAutoGrantSupplies = false;
    private const uint LoginAutoGrantSuppliesCount = 100;

    #region Property
    public Connection? Connection { get; set; }

    public static readonly List<PlayerInstance> _playerInstances = [];
    public int Uid { get; set; }
    public bool Initialized { get; set; }
    public bool IsNewPlayer { get; set; }

    #endregion

    #region Data & Manager

    public PlayerGameData Data { get; } = data;
    public PlayerAttributes Attributes { get; } = new(data);
    public CharacterManager CharacterManager { get; set; } = null!;
    public InventoryManager InventoryManager { get; set; } = null!;
    public LineupManager LineupManager { get; set; } = null!;
    public QuestManager QuestManager { get; set; } = null!;
    public RewardManager RewardManager { get; set; } = null!;
    public RikiManager RikiManager { get; set; } = null!;
    public Rogue3DManager Rogue3DManager { get; set; } = null!;

    private QuestLevelType ActiveLevelType { get; set; }
    private uint ActiveLevelId { get; set; }
    private uint ActiveLevelSeed { get; set; }
    public uint ActiveLevelTeamId { get; private set; }

    #endregion

    #region Initializers
    public PlayerInstance(int uid) : this(new PlayerGameData { Uid = uid })
    {
        // new player
        IsNewPlayer = true;
        Data.Name = PlayerGameData.NormalizeDisplayName(AccountData.GetAccountByUid(uid)?.Username);

        DatabaseHelper.CreateInstance(Data);

        var t = Task.Run(async () =>
        {
            await InitialPlayerManager();
            foreach (var skinCard in GameData.CardSkinData.Values)
            {
                await InventoryManager.AddSkinItem((ItemTypeEnum)skinCard.Genre, skinCard.Detail, skinCard.Particular, skinCard.Level, false);
            }
            foreach (var ar in GameData.ArItemData.Values)
            {
                await InventoryManager.AddArItem((ItemTypeEnum)ar.Genre, ar.Detail, ar.Particular, ar.Level, false);
            }
            foreach (var manifest in GameData.ManifestationData.Values)
            {
                await InventoryManager.AddManifestationItem((ItemTypeEnum)manifest.Genre, manifest.Detail, manifest.Particular, manifest.Level, false);
            }
            foreach (var card in GameData.CardData.Values)
            {
                await CharacterManager.AddCharacter((ItemTypeEnum)card.Genre, card.Detail, card.Particular, card.Level, sendPacket:false);
            }
            var selected = CharacterManager.CharacterData.Characters
                .OrderBy(_ => Guid.NewGuid())
                .Take(3)
                .Select(x => x.Guid)
                .ToList();

            await LineupManager.UpdateLineup(1, selected[0], selected[1], selected[2],false);

        });
        t.Wait();

        Initialized = true;
    }
    private async ValueTask InitialPlayerManager()
    {
        Uid = Data.Uid;
        Data.LastActiveTime = Extensions.GetUnixSec();
        InventoryManager = new InventoryManager(this);
        LineupManager = new LineupManager(this);
        CharacterManager = new CharacterManager(this);
        QuestManager = new QuestManager(this);
        RewardManager = new RewardManager(this);
        RikiManager = new RikiManager(this);
        Rogue3DManager = new Rogue3DManager(this);

        await Task.CompletedTask;
    }
    public T InitializeDatabase<T>() where T : BaseDatabaseDataHelper, new()
    {
        var instance = DatabaseHelper.GetInstanceOrCreateNew<T>(Uid);
        return instance!;
    }

    #endregion

    #region Network
    public async ValueTask OnEnterGame()
    {
        if (!Initialized) await InitialPlayerManager();
        Data.EnsureDisplayName();
        await CharacterManager.RepairCharacterWeapons();
        await EnsureSkins();
        if (EnableLoginAutoGrantSupplies)
            await GrantLoginSupplies();
        EnsureFashionRikiUnlocks();
    }

    public IEnumerable<BaseGameItemInfo> GetSupplyItems() =>
        InventoryManager.InventoryData.Items.Values.Where(x => (x.TemplateId & 0xFFFF) == 5);

    private async ValueTask GrantLoginSupplies()
    {
        foreach (var supplies in GameData.AllSuppliesData)
        {
            await InventoryManager.AddSuppliesItem(supplies, LoginAutoGrantSuppliesCount, false);
        }
    }

    private async ValueTask EnsureSkins()
    {
        foreach (var skinCard in GameData.CardSkinData.Values)
        {
            await InventoryManager.AddSkinItem(
                (ItemTypeEnum)skinCard.Genre,
                skinCard.Detail,
                skinCard.Particular,
                skinCard.Level,
                false);
        }
    }

    private void EnsureFashionRikiUnlocks()
    {
        const uint rikiGroupId = 103;
        var ownedSkinTemplateIds = InventoryManager.InventoryData.Skins.Values
            .Select(x => x.TemplateId)
            .ToHashSet();

        var rikiAttrs = Data.Attrs
            .Where(x => x.Gid == rikiGroupId)
            .ToDictionary(x => x.Sid);

        foreach (var rikiId in LoadUnlockedFashionRikiIds(ownedSkinTemplateIds))
        {
            var (taskId, bit) = GetRikiTask(rikiId);
            var flag = 1u << (int)bit;
            if (rikiAttrs.TryGetValue(taskId, out var attr))
            {
                attr.Val |= flag;
                continue;
            }

            attr = new PlayerAttr
            {
                Gid = rikiGroupId,
                Sid = taskId,
                Val = flag
            };
            Data.Attrs.Add(attr);
            rikiAttrs[taskId] = attr;
        }
    }

    public async ValueTask OnLogin()
    {
        _playerInstances.Add(this);
        await Task.CompletedTask;
    }

    public static PlayerInstance? GetPlayerInstanceByUid(long uid)
        => _playerInstances.FirstOrDefault(player => player.Uid == uid);
    public void OnLogoutAsync()
    {
        _playerInstances.Remove(this);
    }
    public async ValueTask SendPacket(BasePacket packet)
    {
        if (Connection?.IsOnline == true) await Connection.SendPacket(packet);
    }
    public async ValueTask SendPacket(int cmdId, IMessage msg)
    {
        if (Connection?.IsOnline == true) await Connection.SendPacket(cmdId,msg);
    }

    #endregion

    #region Actions
    public async ValueTask OnHeartBeat()
    {
        DatabaseHelper.ToSaveUidList.SafeAdd(Uid);
        await Task.CompletedTask;
    }

    public async ValueTask ReceiveMessage(uint sendUid, uint recvUid, string? message = null, uint? emojiId = null)
    {
        var data = new ChatMsg
        {
            Type = ChatType.Friend,
            Sender = sendUid,
            Recver = recvUid,
            Emoji = emojiId ?? 0,
            Text = ChatMessageHelper.NormalizeForClient(message),
            Profile = Data.ToProfileProto(),
            TimeStamp = ChatMessageHelper.BuildClientTimestamp()
        };

        await SendPacket(CmdIds.NtfFriendChat, data);

        if (recvUid == ConfigManager.Config.ServerOption.ServerProfile.Uid)
        {
            if (message != null)
            {
                if (message.StartsWith("/")) message = message[1..].Trim();
                CommandExecutor.ExecuteCommand(new PlayerCommandSender(this), message);
            }
        }
    }

    #endregion

    #region Serialization

    public PlayerProfile ToServerFriendProto()
    {
        var server = ConfigManager.Config.ServerOption.ServerProfile;
        var proto = new PlayerProfile
        {
            Pid = (uint)server.Uid,
            Account = server.Name,
            Name = server.Name,
            Sex = server.Gender,
            Level = (uint)server.Level,
            Sign = server.Signature
        };
        return proto;
    }

    public Proto.Player ToPlayerProto(bool includeSupportCards = true)
    {
        BuildPlayerAttr();
        BuildPlayerStrAttr();
        var displayName = PlayerGameData.NormalizeDisplayName(Data.Name);
        var proto = new Proto.Player
        {
            Pid = (ulong)Data.Uid,
            Account = displayName,
            Provider = displayName,
            Channel = "gm",
            Subchannel = "gm",
            Name = displayName,
            Level = Data.Level,
            Exp = (uint)Math.Max(0, Data.Exp),
            Sex = Data.Gender,
            Vigor = Data.Vigor,
            Solutions = { LineupManager.LineupData.LineupInfo.Values.Select(x => x.ToProto()) },
            Badges = { InventoryManager.InventoryData.Items.Values.Where(x => x.ItemType == ItemTypeEnum.TYPE_BADGE).Select(x => (ulong)x.UniqueId) }
        };

        foreach (var chara in CharacterManager.CharacterData.Characters) proto.Items.Add(chara.ToProto());
        foreach (var item in InventoryManager.InventoryData.Items.Values) proto.Items.Add(item.ToProto());
        foreach (var skin in InventoryManager.InventoryData.Skins.Values) proto.Items.Add(skin.ToProto());
        foreach (var weapon in InventoryManager.InventoryData.Weapons.Values) proto.Items.Add(weapon.ToProto());
        if (includeSupportCards)
        {
            foreach (var card in InventoryManager.InventoryData.SupportCards.Values) proto.Items.Add(card.ToProto());
        }
        Attributes.SyncTo(proto);

        foreach (var attr in Attributes.AllStrings)
            Attributes.SyncTo(proto, attr);

        foreach (var (key, value) in BuildMoneySync())
        {
            proto.Money[key] = value;
        }

        proto.ShowItems.AddRange(Data.ShowItems);
        proto.ShowAttrs.AddRange(Data.ShowAttrs);

        return proto;
    }

    public void SetDisplayName(string? name)
    {
        Data.Name = PlayerGameData.NormalizeDisplayName(name);
    }

    public void SetShowItem(int index, ulong itemId)
    {
        if (index <= 0)
            return;

        while (Data.ShowItems.Count < index)
            Data.ShowItems.Add(0);

        Data.ShowItems[index - 1] = itemId;
    }

    public void SetShowAttr(int index, uint value)
    {
        if (index <= 0)
            return;

        while (Data.ShowAttrs.Count < index)
            Data.ShowAttrs.Add(0);

        Data.ShowAttrs[index - 1] = value;
    }

    public void SetStrAttr(uint gid, uint sid, string value)
    {
        Attributes.SetString(gid, sid, value);
    }

    public Dictionary<string, int> BuildMoneySync()
    {
        var currentMoney = (int)Math.Min(
            int.MaxValue,
            GetAttrValue(AttrIds.Currency.GroupId, AttrIds.Currency.GetSid(AttrIds.Currency.Money)));
        var sync = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["."] = currentMoney,
            ["gm.gm"] = currentMoney,
            ["jinshan.jinshan"] = currentMoney,
            ["pc_jinshan.pc_jinshan"] = currentMoney
        };
        return sync;
    }

    public void BeginLevelSession(QuestLevelType levelType, uint levelId, uint seed, uint teamId = 0)
    {
        ActiveLevelType = levelType;
        ActiveLevelId = levelId;
        ActiveLevelSeed = seed;
        ActiveLevelTeamId = teamId;
    }

    public bool IsLevelSession(QuestLevelType levelType, uint levelId, uint seed) =>
        ActiveLevelType == levelType && ActiveLevelId == levelId && ActiveLevelSeed == seed;

    public IReadOnlyList<PlayerLevelExcel> AddPlayerExperience(uint amount, NtfSyncPlayer sync)
    {
        if (amount == 0)
            return [];

        if (Data.Level == 0)
            Data.Level = 1;

        var leveledUp = new List<PlayerLevelExcel>();
        var totalExp = (ulong)Math.Max(0, Data.Exp) + amount;
        while (GameData.PlayerLevelData.TryGetValue(Data.Level, out var currentLevel) &&
               currentLevel.MaxExp > 0 &&
               totalExp >= currentLevel.MaxExp &&
               GameData.PlayerLevelData.ContainsKey(Data.Level + 1))
        {
            totalExp -= currentLevel.MaxExp;
            Data.Level++;
            if (GameData.PlayerLevelData.TryGetValue(Data.Level, out var newLevel))
                leveledUp.Add(newLevel);
        }

        Data.Exp = (int)Math.Min(int.MaxValue, totalExp);
        sync.Core[(uint)PlayerCoreAttribute.Level] = Data.Level;
        sync.Core[(uint)PlayerCoreAttribute.Exp] = (uint)Data.Exp;
        return leveledUp;
    }

    public void AddCurrency(uint moneyType, uint amount, NtfSyncPlayer sync)
    {
        if (amount == 0)
            return;

        if (moneyType == AttrIds.Currency.Vigor)
        {
            Data.Vigor = Math.Min(uint.MaxValue - Data.Vigor, amount) + Data.Vigor;
            sync.Core[(uint)PlayerCoreAttribute.Vigor] = Data.Vigor;
            return;
        }

        var sid = AttrIds.Currency.GetSid(moneyType);
        var attr = Attributes.Add(AttrIds.Currency.GroupId, sid, amount);
        Attributes.SyncTo(sync, attr);
        if (moneyType == AttrIds.Currency.Money)
        {
            foreach (var (key, value) in BuildMoneySync())
                sync.Money[key] = value;
        }
    }

    private uint GetAttrValue(uint gid, uint sid)
    {
        return Attributes.GetValue(gid, sid);
    }

    private void BuildPlayerStrAttr()
    {
        foreach (var (gid, sid, value) in BuildShopBootstrapStrAttrs())
        {
            var attr = Data.StrAttrs.FirstOrDefault(x => x.Gid == gid && x.Sid == sid);
            if (attr != null)
            {
                attr.Val = value;
                continue;
            }

            Data.StrAttrs.Add(new PlayerStrAttr
            {
                Gid = gid,
                Sid = sid,
                Val = value
            });
        }
    }

    public void BuildPlayerAttr(bool additional = false)
    {
        QuestManager.MigrateChapterStarAwardMasks();
        QuestManager.RemoveLegacyLevelUnlocks();
        RikiManager.EnsureOwnedItemsUnlocked();

        var bootstrapAttrs = BuildLobbyBootstrapAttrs().ToList();
        if (additional) bootstrapAttrs.AddRange(BuildGirlFurnitureAttrs());
        var seenAttrs = new HashSet<(uint Gid, uint Sid)>();

        foreach (var (gid, sid, value) in bootstrapAttrs)
        {
            if (!seenAttrs.Add((gid, sid)))
                continue;

            var attr = Attributes.Get(gid, sid);
            if (attr != null)
            {
                if (attr.Val < value)
                    attr.Val = value;

                continue;
            }

            Attributes.Set(gid, sid, value);
        }
    }

    private static IEnumerable<(uint Gid, uint Sid, uint Value)> BuildGirlFurnitureAttrs()
    {
        const uint furnitureUnlockedValue = 153391689;
        var groupFurnitureByArea = new Dictionary<uint, uint>();
        foreach (var pos in GameData.HouseFurniturePosData.Values)
        {
            var areaId = pos.AreaId;
            var groupId = pos.GroupId;
            uint selectedIndex = 1;
            var shift = (groupId - 1) * 3;
            if (!groupFurnitureByArea.TryGetValue(areaId, out var packed)) packed = 0;
            packed |= (selectedIndex << (int)shift);
            groupFurnitureByArea[areaId] = packed;
        }

        for (uint girlId = 0; girlId <= 50; girlId++)
        {
            var baseSid = girlId * 50;
            for (uint offset = 10; offset <= 19; offset++)
                yield return (101, baseSid + offset, furnitureUnlockedValue);

            if (groupFurnitureByArea.TryGetValue(girlId, out var groupValue))
                yield return (101, baseSid + 20, groupValue);
        }

        // Massage room furniture
        // 10010..10019
        for (uint sid = 10010; sid <= 10019; sid++)
            yield return (101, sid, furnitureUnlockedValue);

        // Massage room group state
        yield return (101, 10020, 1);

        // Hot spring furniture
        // 15001..15010
        for (uint sid = 15001; sid <= 15010; sid++)
            yield return (101, sid, furnitureUnlockedValue);

        // Beach furniture
        // 17101..17110
        for (uint sid = 17101; sid <= 17110; sid++)
            yield return (101, sid, furnitureUnlockedValue);

        for (uint sid = 30000; sid < 31000; sid++)
            yield return (101, sid, furnitureUnlockedValue);
    }

    private static IEnumerable<uint> LoadUnlockedFashionRikiIds(IReadOnlySet<ulong> ownedSkinTemplateIds)
    {
        const uint fashionType = 5;
        var path = Path.Combine(ConfigManager.Config.Path.ResourcePath, "riki", "Riki.json");
        if (!File.Exists(path))
            yield break;

        if (JsonNode.Parse(File.ReadAllText(path)) is not JsonArray rows)
            yield break;

        foreach (var row in rows.OfType<JsonObject>())
        {
            if (ReadJsonUInt(row["Type"]) != fashionType)
                continue;

            var rikiId = ReadJsonUInt(row["Id"]);
            if (rikiId == 0)
                continue;

            if (ReadRikiConditionTemplateIds(row["Condition"]).Any(ownedSkinTemplateIds.Contains))
                yield return rikiId;
        }
    }

    private static IEnumerable<ulong> ReadRikiConditionTemplateIds(JsonNode? node)
    {
        if (node is not JsonArray array)
            yield break;

        if (array.Count >= 4 && array.Take(4).All(x => x is JsonValue))
        {
            var genre = ReadJsonUInt(array[0]);
            var detail = ReadJsonUInt(array[1]);
            var particular = ReadJsonUInt(array[2]);
            var level = ReadJsonUInt(array[3]);
            if (genre != 0 && detail != 0 && particular != 0 && level != 0)
                yield return GameResourceTemplateId.FromGdpl(genre, detail, particular, level);
        }

        foreach (var child in array)
        {
            foreach (var templateId in ReadRikiConditionTemplateIds(child))
                yield return templateId;
        }
    }

    private static (uint TaskId, uint Bit) GetRikiTask(uint rikiId)
    {
        var index = rikiId / 1000;
        var value = rikiId % 1000;
        var taskId = (index - 1) * 30 + ((value + 29) / 30);
        var bit = value % 30;
        return (taskId, bit);
    }

    private static IEnumerable<(uint Gid, uint Sid, uint Value)> BuildLobbyBootstrapAttrs()
    {
        // GuideLogic uses group 4. Value 999 is safely above every configured step count,
        // so the client treats these guides as already completed.
        yield return (4, 0, 5);
        yield return (11, 1, 1);
        yield return (57, 0, 1);
        yield return (99, 3, 30);
        yield return (110, 1, 1);
        yield return (178, 1, 1_700_000_000);
        yield return (187, 1, 2);

        // Cash.GetMoneyCount uses group 1 with sid = moneyId * 2 + 1 for most currencies.
        // Fill a wide currency id range so every in-game currency starts effectively unlimited.
        for (uint moneyId = 1; moneyId <= 200; moneyId++)
            yield return (1, moneyId * 2 + 1, 999_999);

        for (uint guideId = 1; guideId <= 150; guideId++)
            yield return (4, guideId, 999);

        for (uint guideId = 10_000; guideId <= 10_300; guideId++)
            yield return (4, guideId, 999);

        for (uint guideId = 11_000; guideId <= 11_300; guideId++)
            yield return (4, guideId, 999);

        for (uint guideId = 12_000; guideId <= 12_100; guideId++)
            yield return (4, guideId, 999);

        for (uint guideId = 22_000; guideId <= 22_100; guideId++)
            yield return (4, guideId, 999);

        // Additional guide ids referenced directly by the Lua scripts and observed client logs.
        foreach (var guideId in new uint[] { 10_031, 10_041, 10_061, 10_081, 10_101, 10_224, 11_006, 11_202, 11_210, 22_002 })
            yield return (4, guideId, 999);

        foreach (var guide in GameData.GuideData.Values)
        {
            yield return (4, guide.ID, 999);
        }

        // IBLogic uses group 113 to track whether mall goods have been viewed.
        // Mark every configured mall item as viewed so unimplemented shop content
        // does not keep the main shop button in a red-dot state.
        foreach (var goodsId in GameData.IbGoodsData.Keys)
            yield return (113, goodsId, 1);

        // IBLogic.CheckFreeBox lights the mall button for limited free goods until
        // BuyGroupId reaches LimitTimes. Treat limited mall goods as exhausted
        // while the mall feature is not implemented.
        foreach (var goods in GameData.IbGoodsData.Values)
        {
            if (goods.LimitTimes > 0)
                yield return (26, goods.GoodsId, goods.LimitTimes);
        }

        foreach (var (sid, value) in BuildCompletedFirstRechargeAttrs())
            yield return (50, sid, value);

        foreach (var (gid, sid, value) in BuildCompletedBeginnerSevenDayAttrs())
            yield return (gid, sid, value);

        foreach (var (gid, sid, value) in BuildCompletedNamedBeginnerActivityAttrs())
            yield return (gid, sid, value);

        for (uint favor = 0; favor <= 50; favor++)
            yield return (101, favor * 50, 500);

        // Main Scene 0 mean default scene
        yield return (132, 1, 0);
    }

    private static IEnumerable<(uint Sid, uint Value)> BuildCompletedFirstRechargeAttrs()
    {
        const uint activityIdStep = 10;
        const uint activityFlagPos = 1;
        const uint firstRechargeAwardDiyPos = 3;
        var path = Path.Combine(ConfigManager.Config.Path.ResourcePath, "activity", "activities.json");
        if (!File.Exists(path))
            yield break;

        if (JsonNode.Parse(File.ReadAllText(path)) is not JsonArray rows)
            yield break;

        foreach (var row in rows.OfType<JsonObject>())
        {
            if (!string.Equals(ReadJsonString(row["Class"]), "first_recharge", StringComparison.Ordinal))
                continue;

            var activityId = ReadJsonUInt(row["Id"]);
            if (activityId == 0)
                continue;

            // Bit 0 = red point read, bit 1 = new flag read.
            yield return (activityId * activityIdStep + activityFlagPos, 3);
            // Activity.GetDiyData(activityId, 1) maps to activityId * 10 + 3.
            yield return (activityId * activityIdStep + firstRechargeAwardDiyPos, 1);
        }
    }

    private static IEnumerable<(uint Gid, uint Sid, uint Value)> BuildCompletedBeginnerSevenDayAttrs()
    {
        const uint activityGroupId = 50;
        const uint achievementQuestGroupId = 7;
        const uint activityIdStep = 10;
        const uint activityFlagPos = 1;
        const uint sevenDayUnlockDayDiyPos = 3;
        const uint sevenDayIndexAwardDiyPos = 4;

        foreach (var activityId in LoadBeginnerSevenDayActivityIds())
        {
            var maxDay = LoadSevenDayMaxDay(activityId);
            var indexAwardMask = LoadSevenDayIndexAwardMask(activityId);

            yield return (activityGroupId, activityId * activityIdStep + activityFlagPos, 3);
            yield return (activityGroupId, activityId * activityIdStep + sevenDayUnlockDayDiyPos, maxDay);
            yield return (activityGroupId, activityId * activityIdStep + sevenDayIndexAwardDiyPos, indexAwardMask);

            foreach (var achievementId in LoadSevenDayAchievementIds(activityId))
                yield return (achievementQuestGroupId, achievementId, uint.MaxValue);
        }
    }

    private static IEnumerable<(uint Gid, uint Sid, uint Value)> BuildCompletedNamedBeginnerActivityAttrs()
    {
        const uint activityGroupId = 50;
        const uint achievementQuestGroupId = 7;
        const uint activityIdStep = 10;
        const uint activityFlagPos = 1;
        const uint activityDiyPos1 = 3;
        var completedTitles = new HashSet<string>(StringComparer.Ordinal)
        {
            "activity.Lable13",
            "activity.MianStoryTarget_name",
            "ui.PlayerExp_BuffTitle2"
        };

        foreach (var row in LoadActivityRows())
        {
            if (ReadJsonUInt(row["BeginnerActivities"]) == 0)
                continue;

            if (!completedTitles.Contains(ReadJsonString(row["TitleDes"])))
                continue;

            var activityId = ReadJsonUInt(row["Id"]);
            if (activityId == 0)
                continue;

            yield return (activityGroupId, activityId * activityIdStep + activityFlagPos, 3);

            var activityClass = ReadJsonString(row["Class"]);
            if (string.Equals(activityClass, "diy_ssr", StringComparison.Ordinal))
            {
                yield return (activityGroupId, activityId * activityIdStep + activityDiyPos1, 1);
            }
            else if (string.Equals(activityClass, "activity_quest", StringComparison.Ordinal))
            {
                foreach (var questId in ReadActivityQuestIds(row))
                    yield return (achievementQuestGroupId, questId, uint.MaxValue);
            }
        }
    }

    private static IEnumerable<uint> LoadBeginnerSevenDayActivityIds()
    {
        foreach (var row in LoadActivityRows())
        {
            if (!string.Equals(ReadJsonString(row["Class"]), "seven_day", StringComparison.Ordinal))
                continue;

            if (ReadJsonUInt(row["BeginnerActivities"]) == 0)
                continue;

            var activityId = ReadJsonUInt(row["Id"]);
            if (activityId != 0)
                yield return activityId;
        }
    }

    private static IEnumerable<uint> ReadActivityQuestIds(JsonObject row)
    {
        foreach (var questId in ReadJsonUIntArray(row["Daily"]))
            yield return questId;

        foreach (var questId in ReadJsonUIntArray(row["Weekly"]))
            yield return questId;

        foreach (var questId in ReadJsonUIntArray(row["Normal"]))
            yield return questId;
    }

    private static IEnumerable<JsonObject> LoadActivityRows()
    {
        var path = Path.Combine(ConfigManager.Config.Path.ResourcePath, "activity", "activities.json");
        if (!File.Exists(path))
            yield break;

        if (JsonNode.Parse(File.ReadAllText(path)) is not JsonArray rows)
            yield break;

        foreach (var row in rows.OfType<JsonObject>())
            yield return row;
    }

    private static uint LoadSevenDayMaxDay(uint activityId)
    {
        var path = Path.Combine(ConfigManager.Config.Path.ResourcePath, "activity", "seven_day", "seven_day_achievements.json");
        if (!File.Exists(path))
            return 0;

        if (JsonNode.Parse(File.ReadAllText(path)) is not JsonArray rows)
            return 0;

        uint maxDay = 0;
        foreach (var row in rows.OfType<JsonObject>())
        {
            if (ReadJsonUInt(row["ActId"]) == activityId)
                maxDay = Math.Max(maxDay, ReadJsonUInt(row["DayId"]));
        }

        return maxDay;
    }

    private static uint LoadSevenDayIndexAwardMask(uint activityId)
    {
        var path = Path.Combine(ConfigManager.Config.Path.ResourcePath, "activity", "seven_day", "seven_day_awards.json");
        if (!File.Exists(path))
            return 0;

        if (JsonNode.Parse(File.ReadAllText(path)) is not JsonArray rows)
            return 0;

        uint mask = 0;
        foreach (var row in rows.OfType<JsonObject>())
        {
            if (ReadJsonUInt(row["ActId"]) != activityId)
                continue;

            var awardId = ReadJsonUInt(row["AwardId"]);
            if (awardId is > 0 and < 32)
                mask |= 1u << (int)awardId;
        }

        return mask;
    }

    private static IEnumerable<uint> LoadSevenDayAchievementIds(uint activityId)
    {
        var path = Path.Combine(ConfigManager.Config.Path.ResourcePath, "activity", "seven_day", "seven_day_achievements.json");
        if (!File.Exists(path))
            yield break;

        if (JsonNode.Parse(File.ReadAllText(path)) is not JsonArray rows)
            yield break;

        foreach (var row in rows.OfType<JsonObject>())
        {
            if (ReadJsonUInt(row["ActId"]) != activityId)
                continue;

            if (row["AchieveId"] is not JsonArray achievements)
                continue;

            foreach (var node in achievements)
            {
                var achievementId = ReadJsonUInt(node);
                if (achievementId != 0)
                    yield return achievementId;
            }
        }
    }

    private static IEnumerable<(uint Gid, uint Sid, string Value)> BuildShopBootstrapStrAttrs()
    {
        foreach (var (shopId, version, listVersion) in LoadShopTabVersions())
            yield return (1, shopId, BuildShopDataJson(shopId, version, listVersion));
    }

    private static IEnumerable<(uint ShopId, uint Version, uint ListVersion)> LoadShopTabVersions()
    {
        var path = Path.Combine(ConfigManager.Config.Path.ResourcePath, "shop", "shop_tab.json");
        if (!File.Exists(path))
            yield break;

        if (JsonNode.Parse(File.ReadAllText(path)) is not JsonArray rows)
            yield break;

        foreach (var row in rows.OfType<JsonObject>())
        {
            var shopId = ReadJsonUInt(row["ShopId"]);
            if (shopId == 0)
                continue;

            yield return (shopId, ReadJsonUInt(row["Version"]), ReadJsonUInt(row["ListVersion"]));
        }
    }

    private static uint ReadJsonUInt(JsonNode? node)
    {
        if (node == null)
            return 0;

        if (node is JsonValue value)
        {
            if (value.TryGetValue<uint>(out var uintValue))
                return uintValue;

            if (value.TryGetValue<string>(out var stringValue) && uint.TryParse(stringValue, out var parsed))
                return parsed;
        }

        return 0;
    }

    private static string ReadJsonString(JsonNode? node)
    {
        if (node is JsonValue value && value.TryGetValue<string>(out var stringValue))
            return stringValue;

        return "";
    }

    private static IEnumerable<uint> ReadJsonUIntArray(JsonNode? node)
    {
        if (node is not JsonArray array)
            yield break;

        foreach (var item in array)
        {
            var value = ReadJsonUInt(item);
            if (value != 0)
                yield return value;
        }
    }

    private static string BuildShopDataJson(uint shopId, uint version, uint listVersion)
    {
        var data = new JsonObject
        {
            ["shopid"] = shopId,
            ["version"] = version,
            ["listversion"] = listVersion,
            ["refreshnum"] = 0,
            ["displaynum"] = 0,
            ["refreshtime"] = 0,
            ["tbgoods"] = new JsonArray()
        };

        return data.ToJsonString();
    }
    #endregion
}
