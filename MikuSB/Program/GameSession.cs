using System.Diagnostics;

namespace MikuSB.MikuSB.Program;

internal static class GameSession
{
    public static async Task WaitForExitAsync(int pid, CancellationToken cancellationToken)
    {
        Process game;
        try
        {
            game = Process.GetProcessById(pid);
        }
        catch (ArgumentException)
        {
            // A game that exits immediately may be gone before we obtain its handle.
            return;
        }

        using (game)
        {
            try
            {
                await game.WaitForExitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                try
                {
                    game.Kill(entireProcessTree: true);
                    await game.WaitForExitAsync();
                }
                catch (InvalidOperationException)
                {
                    // The game may have exited at the same time as cancellation.
                }
                throw;
            }
        }
    }
}
