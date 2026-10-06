using System.Globalization;

namespace MikuSB.MikuSB.Program;

public static class ServerLogRotation
{
    public static void Archive(FileInfo file)
    {
        if (!file.Exists) return;
        var start = StartTime(file);
        var name = $"Server-backup-{start:yyyy.MM.dd-HH.mm.ss}";
        var target = Path.Combine(file.DirectoryName!, name + ".log");
        for (var suffix = 1; File.Exists(target); suffix++)
            target = Path.Combine(file.DirectoryName!, $"{name}-{suffix}.log");
        file.MoveTo(target); // Preserve existing archives, including same-second restarts.
    }

    private static DateTime StartTime(FileInfo file)
    {
        var modified = file.LastWriteTime;
        var first = File.ReadLines(file.FullName).FirstOrDefault() ?? "";
        var end = first.IndexOf(']');
        var timestamp = first.StartsWith('[') && end > 0 ? first[1..end] : "";
        if (DateTime.TryParseExact(timestamp, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var start))
            return start;

        // Legacy logs contain only time-of-day. Infer the most recent occurrence
        // before the last write, rather than trusting a copied/inherited creation date.
        if (TimeOnly.TryParseExact(timestamp, "HH:mm:ss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var time))
        {
            start = modified.Date + time.ToTimeSpan();
            return start > modified ? start.AddDays(-1) : start;
        }
        return modified;
    }
}
