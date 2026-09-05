namespace GarageUnderground.Models;

/// <summary>
/// Regole di validazione di un intervento, condivise da interfaccia, servizio e API
/// così che nessun percorso possa scrivere dati non validi.
/// </summary>
public static class InterventoValidator
{
    public const int DescrizioneMaxLength = 2000;

    /// <summary>
    /// Restituisce il messaggio del primo errore trovato, oppure null se i dati sono validi.
    /// </summary>
    public static string? Validate(string? targa, string? descrizione, decimal costo)
    {
        if (string.IsNullOrWhiteSpace(targa))
        {
            return "La targa è obbligatoria";
        }

        if (string.IsNullOrWhiteSpace(descrizione))
        {
            return "La descrizione è obbligatoria";
        }

        if (descrizione.Length > DescrizioneMaxLength)
        {
            return $"La descrizione non può superare {DescrizioneMaxLength} caratteri";
        }

        if (costo < 0)
        {
            return "Il costo non può essere negativo";
        }

        return null;
    }

    public static string? Validate(NuovoInterventoDto dto) =>
        Validate(dto.Targa, dto.Descrizione, dto.Costo);
}
