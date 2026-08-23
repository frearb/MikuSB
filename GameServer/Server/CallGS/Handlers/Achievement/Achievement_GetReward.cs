namespace MikuSB.GameServer.Server.CallGS.Handlers.Achievement;

// Client requests a reward for a completed achievement.
// param: {nId}
// Response: {}
[CallGSApi("Achievement_GetReward")]
public class Achievement_GetReward : CallGSHandler
{
    protected override Task<CallGSResult> HandleAsync(CallGSContext context, string param)
    {
        // TODO: validate achievement completion and grant reward items
        return Task.FromResult(CallGSResult.Ok("{}"));
    }
}
