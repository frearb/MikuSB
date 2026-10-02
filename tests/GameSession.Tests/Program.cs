using System.Diagnostics;
using MikuSB.MikuSB.Program;

if (args.Length > 0 && args[0] == "--child")
{
    await Task.Delay(int.Parse(args[1]));
    return;
}

static Process StartChild(int duration)
{
    return Process.Start(new ProcessStartInfo(Environment.ProcessPath!)
    {
        UseShellExecute = false,
        CreateNoWindow = true,
        ArgumentList = { "--child", duration.ToString() }
    })!;
}

using (var child = StartChild(500))
{
    var wait = GameSession.WaitForExitAsync(child.Id, CancellationToken.None);
    if (wait.IsCompleted)
        throw new Exception("Session ended while the game was still running.");
    await wait.WaitAsync(TimeSpan.FromSeconds(10));
    if (!child.HasExited)
        throw new Exception("Session did not wait for the game to exit.");
    Console.WriteLine("PASS: wait for game exit");
}

using (var child = StartChild(30000))
using (var cancellation = new CancellationTokenSource())
{
    var wait = GameSession.WaitForExitAsync(child.Id, cancellation.Token);
    cancellation.Cancel();
    try
    {
        await wait.WaitAsync(TimeSpan.FromSeconds(10));
        throw new Exception("Cancellation was not propagated.");
    }
    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
    {
    }
    if (!child.HasExited)
        throw new Exception("Game was left running after session cancellation.");
    Console.WriteLine("PASS: cancellation stops the game");
}

using (var child = StartChild(0))
{
    var pid = child.Id;
    await child.WaitForExitAsync();
    await GameSession.WaitForExitAsync(pid, CancellationToken.None);
    Console.WriteLine("PASS: game already exited");
}
