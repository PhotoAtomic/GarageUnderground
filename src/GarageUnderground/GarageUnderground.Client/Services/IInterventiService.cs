using GarageUnderground.Client.Models;

namespace GarageUnderground.Client.Services;

/// <summary>
/// Interfaccia per il servizio di gestione interventi lato client.
/// </summary>
public interface IInterventiService
{
    /// <summary>
    /// Ottiene tutti gli interventi per una specifica targa.
    /// </summary>
    /// <param name="targa">Targa del veicolo.</param>
    /// <returns>Lista degli interventi ordinati per data decrescente.</returns>
    Task<IReadOnlyList<InterventoDto>> GetByTargaAsync(string targa);

    /// <summary>
    /// Crea un nuovo intervento.
    /// </summary>
    /// <param name="intervento">Dati del nuovo intervento.</param>
    /// <returns>L'intervento creato.</returns>
    Task<InterventoDto?> CreateAsync(NuovoInterventoDto intervento);

    /// <summary>
    /// Aggiorna un intervento esistente.
    /// </summary>
    /// <param name="id">ID dell'intervento.</param>
    /// <param name="intervento">Dati aggiornati.</param>
    /// <returns>L'intervento aggiornato.</returns>
    Task<InterventoDto?> UpdateAsync(Guid id, NuovoInterventoDto intervento);

    /// <summary>
    /// Elimina un intervento.
    /// </summary>
    /// <param name="id">ID dell'intervento da eliminare.</param>
    /// <returns>True se eliminato con successo.</returns>
    Task<bool> DeleteAsync(Guid id);
}
