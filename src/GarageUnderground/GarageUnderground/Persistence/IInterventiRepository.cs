using GarageUnderground.Models;

namespace GarageUnderground.Persistence;

/// <summary>
/// Interfaccia per la gestione della persistenza degli interventi.
/// </summary>
public interface IInterventiRepository
{
    /// <summary>
    /// Ottiene tutti gli interventi per una specifica targa, ordinati per data decrescente.
    /// </summary>
    /// <param name="targa">Targa del veicolo.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>Lista degli interventi ordinati per data decrescente.</returns>
    Task<IReadOnlyList<Intervento>> GetByTargaAsync(string targa, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ottiene un intervento per il suo identificatore.
    /// </summary>
    /// <param name="id">Identificatore dell'intervento.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>L'intervento se trovato, null altrimenti.</returns>
    Task<Intervento?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Crea un nuovo intervento.
    /// </summary>
    /// <param name="intervento">Intervento da creare.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>L'intervento creato con l'ID assegnato.</returns>
    Task<Intervento> CreateAsync(Intervento intervento, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggiorna un intervento esistente.
    /// </summary>
    /// <param name="intervento">Intervento aggiornato.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>True se l'aggiornamento è riuscito, false altrimenti.</returns>
    Task<bool> UpdateAsync(Intervento intervento, CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina un intervento.
    /// </summary>
    /// <param name="id">Identificatore dell'intervento da eliminare.</param>
    /// <param name="cancellationToken">Token di cancellazione.</param>
    /// <returns>True se l'eliminazione è riuscita, false altrimenti.</returns>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
