namespace MikuSB.GameServer.Server.CallGS.Handlers.Shop;

// Returns the open/close timestamps for each shop tab.
// Response: {shopId: {nBegin, nEnd}}
[CallGSApi("ShopLogic_GetOpenTime")]
public class ShopLogic_GetOpenTime : CallGSHandler
{
    protected override Task<CallGSResult> HandleAsync(CallGSContext context, string param)
    {
        // TODO: return actual shop open times from config
        return Task.FromResult(CallGSResult.Ok("{}"));
    }
}
