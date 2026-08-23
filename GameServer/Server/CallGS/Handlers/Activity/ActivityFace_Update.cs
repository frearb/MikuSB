namespace MikuSB.GameServer.Server.CallGS.Handlers.Activity;

// Updates the lobby activity face (presentation effect).
// param: {nType(1=equip, 2=unequip, 3=set flag), nId, bFlag}
// Response: {nFaceId, nId, nType} — nFaceId=0 means no next face
[CallGSApi("ActivityFace_Update")]
public class ActivityFace_Update : CallGSHandler
{
    protected override Task<CallGSResult> HandleAsync(CallGSContext context, string param)
    {
        // TODO: process face equip/unequip state and return the next face to display
        return Task.FromResult(CallGSResult.Ok("{\"nFaceId\":0,\"nId\":0,\"nType\":0}"));
    }
}
