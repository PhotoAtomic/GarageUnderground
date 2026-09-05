using GarageUnderground.Persistence;
using LiteDB;

namespace GarageUnderground.Tests;

/// <summary>
/// Database LiteDB su file temporaneo, eliminato a fine test.
/// </summary>
public sealed class TempDatabase : IDisposable
{
    public string Path { get; }
    public ILiteDatabase Database { get; }
    public IDatabaseChangeNotifier Notifier { get; } = new DatabaseChangeNotifier();

    public TempDatabase(string? copyFrom = null)
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"gu-test-{Guid.NewGuid():N}.db");
        if (copyFrom is not null)
        {
            File.Copy(copyFrom, Path);
        }

        Database = new LiteDatabase($"Filename={Path}");
    }

    public LiteDbInterventiRepository Interventi => new(Database, Notifier);
    public LiteDbAutoRepository Auto => new(Database, Notifier);

    public void Dispose()
    {
        Database.Dispose();
        foreach (var file in new[] { Path, System.IO.Path.ChangeExtension(Path, "-log.db") })
        {
            try { File.Delete(file); } catch (IOException) { }
        }
    }
}
