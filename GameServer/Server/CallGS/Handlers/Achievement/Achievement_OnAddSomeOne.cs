namespace MikuSB.GameServer.Server.CallGS.Handlers.Achievement;

// Client notifies the server that an achievement trigger condition was met. No response required.
// param: {nType}
[CallGSApi("Achievement_OnAddSomeOne")]
public class Achievement_OnAddSomeOne : CallGSHandler
{
    protected override Task<CallGSResult> HandleAsync(CallGSContext context, string param)
    {
        // TODO: process achievement progress for the given nType
        return Task.FromResult(CallGSResult.NoResponse());
    }
}
