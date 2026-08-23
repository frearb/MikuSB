namespace MikuSB.GameServer.Server.CallGS.Handlers.Daily;

// Returns daily activity info for each activity type.
// Response: {activityId: tbActivity}
[CallGSApi("Daily_GetActivityInfo")]
public class Daily_GetActivityInfo : CallGSHandler
{
    protected override Task<CallGSResult> HandleAsync(CallGSContext context, string param)
    {
        // TODO: return actual daily activity data
        return Task.FromResult(CallGSResult.Ok("{}"));
    }
}
