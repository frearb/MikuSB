using System.Globalization;
using MikuSB.MikuSB.Program;
using MikuSB.Util;

var directory = Path.Combine(Path.GetTempPath(), "MikuSB-LogRotation-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
var active = Path.Combine(directory, "Server.log");
try
{
    Archive("[22:50:20] [MikuSB] [INFO] Starting", new DateTime(2026, 10, 5, 23, 23, 35), "2026.10.05-22.50.20");
    Archive("[23:59:58] start", new DateTime(2026, 10, 6, 0, 25, 55), "2026.10.05-23.59.58");
    Archive("[2026-10-04 22:00:00] start", new DateTime(2026, 10, 6, 9, 0, 0), "2026.10.04-22.00.00");
    Archive("", new DateTime(2026, 10, 6, 9, 0, 0), "2026.10.06-09.00.00");
    Archive("invalid first line", new DateTime(2026, 10, 6, 9, 0, 0), "2026.10.06-09.00.00-1");
    Archive("[2026-10-04 22:00:00] another start", new DateTime(2026, 10, 6, 9, 0, 0), "2026.10.04-22.00.00-1");
    ServerLogRotation.Archive(new FileInfo(active)); // No previous log is normal.

    Logger.SetLogFile(new FileInfo(active));
    new Logger("RotationTest").Info("timestamp test");
    var line = File.ReadLines(active).First();
    Assert(line.Length > 21 && DateTime.TryParseExact(line[1..20], "yyyy-MM-dd HH:mm:ss",
        CultureInfo.InvariantCulture, DateTimeStyles.None, out _), "Logger must write a full timestamp");
    Console.WriteLine("PASS: inherited creation date, legacy midnight rollover, full start date, malformed/empty logs, collision preservation, and logger timestamps");
}
finally
{
    foreach (var file in Directory.EnumerateFiles(directory)) File.Delete(file);
    Directory.Delete(directory);
}

void Archive(string contents, DateTime modified, string expected)
{
    File.WriteAllText(active, contents);
    File.SetCreationTime(active, new DateTime(2026, 5, 17));
    File.SetLastWriteTime(active, modified);
    ServerLogRotation.Archive(new FileInfo(active));
    var archived = Path.Combine(directory, "Server-backup-" + expected + ".log");
    Assert(!File.Exists(active) && File.ReadAllText(archived) == contents, "Archive must preserve content under the expected date");
    Assert(File.ReadAllText(Path.Combine(directory, "Server-backup-2026.10.05-22.50.20.log")) == "[22:50:20] [MikuSB] [INFO] Starting",
        "Existing archives must never be overwritten");
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
