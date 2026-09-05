using GarageUnderground.Models;

namespace GarageUnderground.Persistence;

/// <summary>
/// Persistenza delle schede auto.
/// </summary>
public interface IAutoRepository
{
    /// <summary>
    /// Scheda per targa (normalizzata internamente), null se non esiste.
    /// </summary>
    Task<Auto?> GetByTargaAsync(string targa, CancellationToken cancellationToken = default);

    Task<Auto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea la scheda. Restituisce null se esiste già una scheda per quella targa.
    /// </summary>
    Task<Auto?> CreateAsync(Auto auto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggiorna la scheda. False se non esiste o se la nuova targa è già usata da un'altra scheda.
    /// </summary>
    Task<bool> UpdateAsync(Auto auto, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
