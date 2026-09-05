using GarageUnderground.Models;

namespace GarageUnderground.Services.Client;

/// <summary>
/// Servizio per la scheda anagrafica delle auto, usato dalle pagine.
/// </summary>
public interface IAutoService
{
    /// <summary>
    /// Scheda per targa, null se non ancora inserita.
    /// </summary>
    Task<AutoDto?> GetByTargaAsync(string targa);

    Task<AutoDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Crea la scheda. Null se i dati non sono validi o la targa esiste già.
    /// </summary>
    Task<AutoDto?> CreateAsync(SalvaAutoDto auto);

    /// <summary>
    /// Aggiorna la scheda. Null se i dati non sono validi o la scheda non esiste.
    /// </summary>
    Task<AutoDto?> UpdateAsync(Guid id, SalvaAutoDto auto);

    Task<bool> DeleteAsync(Guid id);
}
