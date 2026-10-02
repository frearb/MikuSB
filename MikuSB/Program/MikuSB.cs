using MikuSB.Data;
using MikuSB.Database;
using MikuSB.MikuSB.Tool;
using MikuSB.GameServer.Command;
using MikuSB.GameServer.Server;
using MikuSB.Internationalization;
using MikuSB.MikuSB.Update;
using MikuSB.TcpSharp;
using MikuSB.Util;
using System.Globalization;
using MikuSB.Loader;

namespace MikuSB.MikuSB.Program;

public class MikuSB
{
    public static readonly Logger Logger = new("MikuSB");
    public static readonly DatabaseHelper DatabaseHelper = new();
    public static readonly Listener Listener = new();
    public static readonly CommandManager CommandManager = new();

    // for exit signal
    private static readonly CancellationTokenSource _cts = new();
    private static int _exitCode = 0;

    public static async Task Main(string[] args)
    {
        var play = args.Length > 0 && args[0].Equals("--play", StringComparison.OrdinalIgnoreCase);
        if (play)
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        var time = DateTime.Now;
        IConsole.InitConsole();
        RegisterExitEvent();
        try
        {
            LoaderManager.InitConfig();
            ShowAntiScamWarning();
            if (!play && await UpdateService.TryStartSelfUpdateAsync())
                return;

            await LoaderManager.InitSdkServer();
            LoaderManager.InitPacket();

            await LoaderManager.InitDatabase(_cts.Token);

            Logger.Warn(I18NManager.Translate("Server.ServerInfo.WaitForAllDone"));

            await LoaderManager.InitResource();
            ResourceManager.IsLoaded = true;

            HandbookGenerator.GenerateAll();
            // Register commands before launching, including the optional in-game console bridge.
            await LoaderManager.InitCommand(_cts.Token, listenConsole: false);

            var elapsed = DateTime.Now - time;
            Logger.Info(I18NManager.Translate("Server.ServerInfo.ServerStarted",
                Math.Round(elapsed.TotalSeconds, 2).ToString(CultureInfo.InvariantCulture)));

            if (play)
            {
                _cts.Token.ThrowIfCancellationRequested();
                var pid = GameLaunchService.Launch(args.Skip(1).ToArray());
                Logger.Info($"Game started (PID {pid}). The server will stop when the game exits.");
                await GameSession.WaitForExitAsync(pid, _cts.Token);
                Logger.Info("Game exited. Saving data and stopping the server.");
                RequestShutdown(0);
            }
            else
            {
                await IConsole.ListenConsole(_cts.Token);
            }
        }
        catch (OperationCanceledException) when (_cts.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Logger.Error("Server or game startup failed.", ex);
            RequestShutdown(1);
        }

        await ProcessExit(Volatile.Read(ref _exitCode));
    }

    private static void ShowAntiScamWarning()
    {
        Logger.Warn("============================================================");
        Logger.Warn("MikuSB is completely free and open source.");
        Logger.Warn("If you paid anyone for this server, you were scammed.");
        Logger.Warn("Request a refund immediately and report the seller to us.");
        Logger.Warn("Discord: https://discord.gg/aMwCu9JyUR");
        Logger.Warn("============================================================");
    }

    #region Exit

    private static void RegisterExitEvent()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            Logger.Info(I18NManager.Translate("Server.ServerInfo.Shutdown"));
            RequestShutdown(0);
        };
        AppDomain.CurrentDomain.UnhandledException += (obj, arg) =>
        {
            Logger.Error(I18NManager.Translate("Server.ServerInfo.UnhandledException", obj.GetType().Name),
                (Exception)arg.ExceptionObject);
            Logger.Info(I18NManager.Translate("Server.ServerInfo.Shutdown"));
            RequestShutdown(1);
        };

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            Logger.Info(I18NManager.Translate("Server.ServerInfo.CancelKeyPressed"));
            eventArgs.Cancel = true;
            RequestShutdown(0);
        };
    }

    private static void RequestShutdown(int exitCode)
    {
        Interlocked.Exchange(ref _exitCode, exitCode);
        _cts.Cancel();
    }

    private static async Task ProcessExit(int exitCode)
    {
        SocketListener.StopListener();
        try
        {
            await SdkServer.SdkServer.StopAsync();
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to stop HTTP/proxy services.", ex);
            exitCode = 1;
        }
        SocketListener.Connections.Values.ToList().ForEach(x => x.Stop(true));

        DatabaseHelper.Stop();          // notify stop
        await DatabaseHelper.WaitAsync(); // wait AutoSave thread exit

        if (DatabaseHelper.LoadAllData)
            DatabaseHelper.SaveDatabase(); // final flush

        Environment.Exit(exitCode);
    }

    # endregion
}
