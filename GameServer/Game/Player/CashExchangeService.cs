using System.Text.Json;
using MikuSB.Data;
using MikuSB.GameServer.Server.CallGS;
using MikuSB.GameServer.Server.CallGS.Handlers.Cash;
using MikuSB.Proto;

namespace MikuSB.GameServer.Game.Player;

internal static class CashExchangeService
{
    // Use the same 04:00 UTC+8 game-day boundary as the house weekly reset.
    internal static uint Day(DateTimeOffset now) =>
        (uint)DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(8)).AddHours(-4).DateTime).DayNumber;

    internal static bool Refresh(PlayerAttributes attrs, DateTimeOffset now, NtfSyncPlayer? sync = null)
    {
        var day = Day(now);
        var previous = attrs.GetValue(AttrIds.CashExchange.GroupId, AttrIds.CashExchange.RefreshDaySid);
        if (previous >= day) return false;
        // Adopt existing counters on first use; reconnecting must never refill them.
        if (previous != 0)
        {
            foreach (var sid in new[] { AttrIds.CashExchange.VigorLimitSid, AttrIds.CashExchange.SilverLimitSid })
            {
                var attr = attrs.Set(AttrIds.CashExchange.GroupId, sid, 0);
                if (sync != null) attrs.SyncTo(sync, attr);
            }
        }
        attrs.Set(AttrIds.CashExchange.GroupId, AttrIds.CashExchange.RefreshDaySid, day);
        return previous != 0;
    }

    internal static CallGSResult Exchange(PlayerInstance player, CashExchangeParam req, DateTimeOffset now)
    {
        var sync = new NtfSyncPlayer();
        if (req.Source != AttrIds.Currency.Gold || req.Target is not (AttrIds.Currency.Silver or AttrIds.Currency.Vigor)
            || req.Cost == 0)
            return CallGSResult.Error("error.BadParam");

        lock (player.Data)
        {
            Refresh(player.Attributes, now, sync);
            var sid = req.Target == AttrIds.Currency.Silver
                ? AttrIds.CashExchange.SilverLimitSid : AttrIds.CashExchange.VigorLimitSid;
            var count = player.Attributes.GetValue(AttrIds.CashExchange.GroupId, sid);
            uint cost, amount;
            if (count == uint.MaxValue) return CallGSResult.Error("tip.exchange_invaild", sync);
            if (req.Target == AttrIds.Currency.Silver && GameData.SilverExchangeData.TryGetValue(count + 1, out var silver))
                (cost, amount) = (silver.Cost, silver.Silver);
            else if (req.Target == AttrIds.Currency.Vigor && GameData.VigorExchangeData.TryGetValue(count + 1, out var vigor))
                (cost, amount) = (vigor.Cost, vigor.Vigor);
            else
                return CallGSResult.Error("tip.exchange_invaild", sync);

            if (cost == 0 || amount == 0 || req.Cost != cost)
                return CallGSResult.Error("error.BadParam", sync);
            var balance = req.Target == AttrIds.Currency.Vigor ? player.Data.Vigor : player.GetCurrencyBalance(req.Target);
            if (amount > uint.MaxValue - balance)
                return CallGSResult.Error("error.BadParam", sync);
            if (!player.TrySpendCurrency(AttrIds.Currency.Gold, cost, sync))
                return CallGSResult.Error("tip.cash_not_enough", sync);

            player.AddCurrency(req.Target, amount, sync);
            player.Attributes.SyncTo(sync, player.Attributes.Set(AttrIds.CashExchange.GroupId, sid, count + 1));
            return CallGSResult.Ok(JsonSerializer.Serialize(new { nCashType = req.Target, nCount = amount }), sync);
        }
    }
}
