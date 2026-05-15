using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using MikuSB.Database;
using MikuSB.Database.Player;
using MikuSB.GameServer.Game.Player;
using MikuSB.Proto;

namespace MikuSB.GameServer.Server.CallGS.Handlers.Boss;

internal static class BossPvpLogicState
{
    private const uint GroupId = 51;
    private const uint ActivitySubId = 0;
    private const uint ChallengeNumSid = 1;
    private const uint DiffStartId = 10;
    private const uint StartSid = 100;
    private const uint SubNum = 10;
    private const uint DefaultOpenId = 1;
    private const uint DefaultChallengeNum = 400;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = null };
    private static readonly ConcurrentDictionary<int, uint> LastTeamByPlayer = [];

    public static NtfSyncPlayer EnsureOpenState(PlayerInstance player)
    {
        var sync = new NtfSyncPlayer();
        SetStrAttr(player, ActivitySubId, DefaultOpenId.ToString(), sync);
        EnsureMinStrNumber(player, ChallengeNumSid, DefaultChallengeNum, sync);
        DatabaseHelper.SaveDatabaseType(player.Data);
        return sync;
    }

    public static async Task SendStrAttrsAsync(Connection connection)
    {
        var player = connection.Player!;
        foreach (var attr in player.Data.StrAttrs.Where(x => x.Gid == GroupId).OrderBy(x => x.Sid))
        {
            await player.SendPacket(CmdIds.NtfSetStrAttr, new NtfSetStrAttr
            {
                Gid = attr.Gid,
                Sid = attr.Sid,
                Val = attr.Val
            });
        }
    }

    public static NtfSyncPlayer RecordSettlement(PlayerInstance player, BossPvpSettlementParam req)
    {
        var sync = EnsureOpenState(player);
        var bossId = req.Id;
        if (bossId == 0)
        {
            return sync;
        }

        var baseSid = GetBossBaseSid(bossId);
        var oldIntegral = GetStrNumber(player, baseSid + 4);
        var newIntegral = Math.Max(oldIntegral, BuildIntegral(req));
        var finishTime = req.Time > 0 ? (uint)Math.Ceiling(req.Time) : GetStrNumber(player, baseSid + 5);

        SetStrAttr(player, baseSid + 4, newIntegral.ToString(), sync);
        SetStrAttr(player, baseSid + 5, finishTime.ToString(), sync);
        IncrementStrNumber(player, baseSid + 6, sync);
        IncrementStrNumber(player, baseSid + 7, sync);
        SetStrAttr(player, baseSid + 8, Math.Max(GetStrNumber(player, baseSid + 8), req.Diff).ToString(), sync);

        var teamId = req.TeamId != 0 ? req.TeamId : LastTeamByPlayer.GetValueOrDefault(player.Uid, 1u);
        SaveLineup(player, bossId, teamId, sync);

        DatabaseHelper.SaveDatabaseType(player.Data);
        return sync;
    }

    public static NtfSyncPlayer RecordFail(PlayerInstance player, BossPvpFailParam req)
    {
        var sync = EnsureOpenState(player);
        if (req.Id != 0)
        {
            IncrementStrNumber(player, GetBossBaseSid(req.Id) + 7, sync);
        }

        DatabaseHelper.SaveDatabaseType(player.Data);
        return sync;
    }

    public static NtfSyncPlayer RecordMopup(PlayerInstance player, BossPvpMopupParam req)
    {
        var settlement = new BossPvpSettlementParam
        {
            Id = req.Id,
            Diff = req.Diff,
            ResidueTime = 0,
            Time = 0
        };
        return RecordSettlement(player, settlement);
    }

    public static void RecordEnter(PlayerInstance player, BossPvpEnterParam req)
    {
        if (req.TeamId != 0)
        {
            LastTeamByPlayer[player.Uid] = req.TeamId;
        }
    }

    public static int ClearLineupLocks(PlayerInstance player, NtfSyncPlayer sync)
    {
        var emptyRoleData = EmptyRoleData().ToJsonString(JsonOptions);
        var roleAttrs = player.Data.StrAttrs
            .Where(x => x.Gid == GroupId && IsLineupLockSid(x.Sid) && x.Val != emptyRoleData)
            .Select(x => x.Sid)
            .ToArray();

        foreach (var sid in roleAttrs)
        {
            SetStrAttr(player, sid, emptyRoleData, sync);
        }

        if (roleAttrs.Length > 0)
        {
            DatabaseHelper.SaveDatabaseType(player.Data);
        }

        return roleAttrs.Length;
    }

    public static JsonNode HandleReconnectSettlement(PlayerInstance player, string? command, JsonNode? payload, out NtfSyncPlayer? sync)
    {
        sync = null;
        if (string.Equals(command, "BossPvpLogic_LevelSettlement", StringComparison.Ordinal))
        {
            var req = payload.Deserialize<BossPvpSettlementParam>(JsonOptions) ?? new BossPvpSettlementParam();
            sync = RecordSettlement(player, req);
            return new JsonObject();
        }

        if (string.Equals(command, "BossPvpLogic_LevelFail", StringComparison.Ordinal))
        {
            var req = payload.Deserialize<BossPvpFailParam>(JsonOptions) ?? new BossPvpFailParam();
            sync = RecordFail(player, req);
            return new JsonObject();
        }

        return payload?.DeepClone() ?? new JsonObject();
    }

    private static uint BuildIntegral(BossPvpSettlementParam req)
    {
        var diff = Math.Max(req.Diff, 1);
        var timeBonus = req.ResidueTime > 0 ? (uint)Math.Ceiling(req.ResidueTime) : 0;
        return diff * 1000 + timeBonus;
    }

    private static uint GetBossBaseSid(uint bossId) => SubNum * bossId + StartSid;

    private static bool IsLineupLockSid(uint sid)
    {
        if (sid < StartSid)
        {
            return false;
        }

        var offset = (sid - StartSid) % SubNum;
        return offset is >= 1 and <= 3;
    }

    private static void SaveLineup(PlayerInstance player, uint bossId, uint teamId, NtfSyncPlayer sync)
    {
        var lineupId = teamId == 0 ? 1 : (int)teamId;
        if (!player.LineupManager.LineupData.LineupInfo.TryGetValue(lineupId, out var lineup))
        {
            return;
        }

        var members = new[] { lineup.Member1, lineup.Member2, lineup.Member3 };
        for (var i = 0; i < members.Length; i++)
        {
            var roleData = BuildRoleData(player, members[i]);
            SetStrAttr(player, GetBossBaseSid(bossId) + (uint)i + 1, roleData.ToJsonString(JsonOptions), sync);
        }
    }

    private static JsonObject BuildRoleData(PlayerInstance player, uint characterGuid)
    {
        var roleData = EmptyRoleData();
        if (characterGuid == 0)
        {
            return roleData;
        }

        var character = player.CharacterManager.GetCharacterByGUID(characterGuid);
        if (character == null)
        {
            return roleData;
        }

        roleData["role"] = character.Guid;
        var weapon = player.InventoryManager.GetWeaponItem(character.WeaponUniqueId);
        if (weapon != null)
        {
            roleData["weapon"] = weapon.UniqueId;
            roleData["wgdpl"] = BuildGdplArray(weapon.TemplateId, weapon.Level, weapon.Evolue);
            roleData["wslot"] = BuildSlotObject(weapon.PartSlots);
        }

        var supportUids = character.SupportSlots
            .OrderBy(x => x.Key)
            .Select(x => x.Value)
            .Where(x => x != 0)
            .Take(3)
            .ToArray();

        for (var i = 0; i < supportUids.Length; i++)
        {
            var support = player.InventoryManager.GetSupportCardItem(supportUids[i]);
            if (support == null)
            {
                continue;
            }

            var index = i + 1;
            roleData[$"s{index}"] = support.UniqueId;
            roleData[$"sgdpl{index}"] = BuildGdplArray(support.TemplateId, support.Level, 0);
        }

        return roleData;
    }

    private static JsonObject EmptyRoleData() => new()
    {
        ["role"] = 0,
        ["weapon"] = 0,
        ["s1"] = 0,
        ["s2"] = 0,
        ["s3"] = 0,
        ["wgdpl"] = new JsonArray(),
        ["wslot"] = new JsonObject(),
        ["sgdpl1"] = new JsonArray(),
        ["sgdpl2"] = new JsonArray(),
        ["sgdpl3"] = new JsonArray()
    };

    private static JsonArray BuildGdplArray(ulong templateId, uint enhanceLevel, uint evolveOrBreak)
    {
        var gdpl = SplitGdpl(templateId);
        return [gdpl.Genre, gdpl.Detail, gdpl.Particular, gdpl.Level, enhanceLevel, evolveOrBreak];
    }

    private static JsonObject BuildSlotObject(Dictionary<uint, ulong> slots)
    {
        var result = new JsonObject();
        foreach (var (slot, uid) in slots.OrderBy(x => x.Key))
        {
            result[slot.ToString()] = uid;
        }

        return result;
    }

    private static (uint Genre, uint Detail, uint Particular, uint Level) SplitGdpl(ulong templateId)
    {
        return (
            (uint)(templateId & 0xFFFF),
            (uint)((templateId >> 16) & 0xFFFF),
            (uint)((templateId >> 32) & 0xFFFF),
            (uint)((templateId >> 48) & 0xFFFF));
    }

    private static void EnsureMinStrNumber(PlayerInstance player, uint sid, uint value, NtfSyncPlayer sync)
    {
        if (GetStrNumber(player, sid) >= value)
        {
            return;
        }

        SetStrAttr(player, sid, value.ToString(), sync);
    }

    private static void IncrementStrNumber(PlayerInstance player, uint sid, NtfSyncPlayer sync)
    {
        SetStrAttr(player, sid, (GetStrNumber(player, sid) + 1).ToString(), sync);
    }

    private static uint GetStrNumber(PlayerInstance player, uint sid)
    {
        var value = player.Data.StrAttrs.FirstOrDefault(x => x.Gid == GroupId && x.Sid == sid)?.Val;
        return uint.TryParse(value, out var parsed) ? parsed : 0;
    }

    private static void SetStrAttr(PlayerInstance player, uint sid, string value, NtfSyncPlayer sync)
    {
        var attrs = player.Data.StrAttrs.Where(x => x.Gid == GroupId && x.Sid == sid).ToList();
        if (attrs.Count == 0)
        {
            player.Data.StrAttrs.Add(new PlayerStrAttr { Gid = GroupId, Sid = sid, Val = value });
        }
        else
        {
            attrs[0].Val = value;
            foreach (var duplicate in attrs.Skip(1))
            {
                player.Data.StrAttrs.Remove(duplicate);
            }
        }

        sync.CustomStr[player.ToShiftedAttrKey(GroupId, sid)] = value;
    }
}

[CallGSApi("BossPvpLogic_EnterLevel")]
public class BossPvpLogic_EnterLevel : ICallGSHandler
{
    private static readonly Random Random = new();

    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var sync = BossPvpLogicState.EnsureOpenState(connection.Player!);
        var req = JsonSerializer.Deserialize<BossPvpEnterParam>(param) ?? new BossPvpEnterParam();
        BossPvpLogicState.RecordEnter(connection.Player!, req);
        var rsp = $"{{\"nSeed\":{Random.Next(1, 1_000_000_000)}}}";
        await BossPvpLogicState.SendStrAttrsAsync(connection);
        await CallGSRouter.SendScript(connection, "BossPvpLogic_EnterLevel", rsp, sync);
    }
}

[CallGSApi("BossPvpLogic_GetOpenID")]
public class BossPvpLogic_GetOpenID : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var sync = BossPvpLogicState.EnsureOpenState(connection.Player!);
        await BossPvpLogicState.SendStrAttrsAsync(connection);

        // BossPvp season selector. Client (BossPvp.lua:948-973) only treats
        // the activity as open when `nID` names a season configured in
        // challenge/bosspvp/boss_challenge.txt; absent it shows TxtNotOpen
        // and bails. Pin id=1 - the master config row, 42 bosses configured.
        //
        // tbTimeCfg is the GM time-window override path (line 955-961). The
        // local table is keyed by numeric int (tonumber(tbLine.ID)) but
        // JSON4Lua (Misc/Json.lua) decodes JSON object keys as strings, so
        // the `{ "1": {...} }` form never matches. An ARRAY works around
        // this: JSON4Lua decodes `[x]` to `{[1]=x}` with numeric indices, so
        // `for id, cfg in pairs(...)` yields id=1 and the lookup hits. -1/-1
        // are IsInTime's "no bound" sentinels (Misc/Lib.lua:506-509), which
        // makes downstream settlement-time IsInTime checks pass against the
        // real-clock now.
        var rsp = "{\"nID\": 1, \"tbTimeCfg\":[{\"nStartTime\":-1,\"nEndTime\":-1}]}";
        await CallGSRouter.SendScript(connection, "BossPvpLogic_GetOpenID", rsp, sync);
    }
}

[CallGSApi("BossPvpLogic_Record")]
public class BossPvpLogic_Record : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<BossPvpRecordParam>(param) ?? new BossPvpRecordParam();
        var rsp = JsonSerializer.Serialize(new { bRecord = req.Record });
        await CallGSRouter.SendScript(connection, "BossPvpLogic_Record", rsp);
    }
}

[CallGSApi("BossPvpLogic_LevelMopup")]
public class BossPvpLogic_LevelMopup : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        var req = JsonSerializer.Deserialize<BossPvpMopupParam>(param) ?? new BossPvpMopupParam();
        var sync = BossPvpLogicState.RecordMopup(connection.Player!, req);
        await BossPvpLogicState.SendStrAttrsAsync(connection);
        await CallGSRouter.SendScript(connection, "BossPvpLogic_LevelMopup", "{}", sync);
    }
}

[CallGSApi("BossPvpLogic_GetReward")]
public class BossPvpLogic_GetReward : ICallGSHandler
{
    public async Task Handle(Connection connection, string param, ushort seqNo)
    {
        await CallGSRouter.SendScript(connection, "BossPvpLogic_GetReward", "{\"tbAward\":[]}");
    }
}

internal sealed class BossPvpSettlementParam
{
    [JsonPropertyName("nID")]
    public uint Id { get; set; }

    [JsonPropertyName("nDiff")]
    public uint Diff { get; set; }

    [JsonPropertyName("nDifficulty")]
    public uint Difficulty { set => Diff = value; }

    [JsonPropertyName("nTime")]
    public double Time { get; set; }

    [JsonPropertyName("ResidueTime")]
    public double ResidueTime { get; set; }

    [JsonPropertyName("nTeamID")]
    public uint TeamId { get; set; }
}

internal sealed class BossPvpEnterParam
{
    [JsonPropertyName("nTeamID")]
    public uint TeamId { get; set; }
}

internal sealed class BossPvpFailParam
{
    [JsonPropertyName("nID")]
    public uint Id { get; set; }
}

internal sealed class BossPvpMopupParam
{
    [JsonPropertyName("nID")]
    public uint Id { get; set; }

    [JsonPropertyName("nDiff")]
    public uint Diff { get; set; }
}

internal sealed class BossPvpRecordParam
{
    [JsonPropertyName("bRecord")]
    public bool Record { get; set; }
}
