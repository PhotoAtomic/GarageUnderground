using GarageUnderground.Models;
using LiteDB;

namespace GarageUnderground.Persistence;

/// <summary>
/// Implementazione del repository degli interventi usando LiteDB.
/// </summary>
public sealed class LiteDbInterventiRepository : IInterventiRepository
{
    private const string CollectionName = "interventi";
    private readonly ILiteDatabase database;
    private readonly IDatabaseChangeNotifier changeNotifier;

    public LiteDbInterventiRepository(ILiteDatabase database, IDatabaseChangeNotifier changeNotifier)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
        this.changeNotifier = changeNotifier ?? throw new ArgumentNullException(nameof(changeNotifier));

        // Configura indici per ottimizzare le query
        var collection = this.database.GetCollection<Intervento>(CollectionName);
        collection.EnsureIndex(x => x.Targa);
        collection.EnsureIndex(x => x.Data);
    }

    public Task<IReadOnlyList<Intervento>> GetByTargaAsync(string targa, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targa))
        {
            return Task.FromResult<IReadOnlyList<Intervento>>([]);
        }

        var normalizedTarga = NormalizeTarga(targa);
        var collection = database.GetCollection<Intervento>(CollectionName);

        var interventi = collection
            .Find(x => x.Targa == normalizedTarga)
            .OrderByDescending(x => x.Data)
            .ThenByDescending(x => x.CreatedAt)
            .ToList();

        return Task.FromResult<IReadOnlyList<Intervento>>(interventi);
    }

    public Task<Intervento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<Intervento>(CollectionName);
        var intervento = collection.FindById(id);
        return Task.FromResult(intervento);
    }

    public Task<Intervento> CreateAsync(Intervento intervento, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intervento);

        var normalizedIntervento = intervento with
        {
            Targa = NormalizeTarga(intervento.Targa)
        };

        var collection = database.GetCollection<Intervento>(CollectionName);
        collection.Insert(normalizedIntervento);

        // Notifica che ci sono modifiche da persistere
        changeNotifier.NotifyChange();

        return Task.FromResult(normalizedIntervento);
    }

    public Task<bool> UpdateAsync(Intervento intervento, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(intervento);

        var normalizedIntervento = intervento with
        {
            Targa = NormalizeTarga(intervento.Targa)
        };

        var collection = database.GetCollection<Intervento>(CollectionName);
        var updated = collection.Update(normalizedIntervento);

        // Notifica che ci sono modifiche da persistere
        if (updated)
        {
            changeNotifier.NotifyChange();
        }

        return Task.FromResult(updated);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var collection = database.GetCollection<Intervento>(CollectionName);
        var deleted = collection.Delete(id);

        // Notifica che ci sono modifiche da persistere
        if (deleted)
        {
            changeNotifier.NotifyChange();
        }

        return Task.FromResult(deleted);
    }

    private static string NormalizeTarga(string targa)
    {
        return targa.Trim().ToUpperInvariant().Replace(" ", "");
    }
}
