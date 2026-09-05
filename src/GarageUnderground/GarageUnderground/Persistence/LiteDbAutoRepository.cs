using GarageUnderground.Models;
using LiteDB;

namespace GarageUnderground.Persistence;

/// <summary>
/// Repository delle schede auto su LiteDB. Collezione separata da "interventi":
/// i dati esistenti non vengono toccati.
/// </summary>
public sealed class LiteDbAutoRepository : IAutoRepository
{
    private const string CollectionName = "auto";
    private readonly ILiteDatabase database;
    private readonly IDatabaseChangeNotifier changeNotifier;

    public LiteDbAutoRepository(ILiteDatabase database, IDatabaseChangeNotifier changeNotifier)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
        this.changeNotifier = changeNotifier ?? throw new ArgumentNullException(nameof(changeNotifier));

        Collection.EnsureIndex(x => x.Targa, unique: true);
    }

    private ILiteCollection<Auto> Collection => database.GetCollection<Auto>(CollectionName);

    public Task<Auto?> GetByTargaAsync(string targa, CancellationToken cancellationToken = default)
    {
        var normalized = TargaNormalizer.Normalize(targa);
        if (normalized.Length == 0)
        {
            return Task.FromResult<Auto?>(null);
        }

        return Task.FromResult<Auto?>(Collection.FindOne(x => x.Targa == normalized));
    }

    public Task<Auto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Auto?>(Collection.FindById(id));
    }

    public Task<Auto?> CreateAsync(Auto auto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auto);

        var normalized = auto with { Targa = TargaNormalizer.Normalize(auto.Targa) };
        if (Collection.Exists(x => x.Targa == normalized.Targa))
        {
            return Task.FromResult<Auto?>(null);
        }

        Collection.Insert(normalized);
        changeNotifier.NotifyChange();

        return Task.FromResult<Auto?>(normalized);
    }

    public Task<bool> UpdateAsync(Auto auto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auto);

        var normalized = auto with { Targa = TargaNormalizer.Normalize(auto.Targa) };
        if (Collection.Exists(x => x.Targa == normalized.Targa && x.Id != normalized.Id))
        {
            return Task.FromResult(false);
        }

        var updated = Collection.Update(normalized);
        if (updated)
        {
            changeNotifier.NotifyChange();
        }

        return Task.FromResult(updated);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = Collection.Delete(id);
        if (deleted)
        {
            changeNotifier.NotifyChange();
        }

        return Task.FromResult(deleted);
    }
}
