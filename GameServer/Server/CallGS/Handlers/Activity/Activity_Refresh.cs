namespace MikuSB.GameServer.Server.CallGS.Handlers.Activity;

// Client requests an activity state refresh. No s2c callback registered on client side.
// param: {nId}
[CallGSApi("Activity_Refresh")]
public class Activity_Refresh : CallGSHandler
{
    protected override Task<CallGSResult> HandleAsync(CallGSContext context, string param)
    {
        // TODO: refresh activity state for the given nId
        return Task.FromResult(CallGSResult.NoResponse());
    }
}
