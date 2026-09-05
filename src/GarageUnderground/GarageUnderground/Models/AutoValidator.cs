namespace GarageUnderground.Models;

/// <summary>
/// Regole di validazione della scheda auto, condivise da interfaccia, servizio e API.
/// </summary>
public static class AutoValidator
{
    public const int TestoMaxLength = 100;
    public const int NoteMaxLength = 2000;
    public const int AnnoMinimo = 1900;

    /// <summary>
    /// Restituisce il messaggio del primo errore trovato, oppure null se i dati sono validi.
    /// </summary>
    public static string? Validate(SalvaAutoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Targa))
        {
            return "La targa è obbligatoria";
        }

        var annoMassimo = DateTime.UtcNow.Year + 1;
        if (dto.Anno is < AnnoMinimo or > int.MaxValue || (dto.Anno is int anno && anno > annoMassimo))
        {
            return $"L'anno deve essere compreso tra {AnnoMinimo} e {annoMassimo}";
        }

        if (dto.Cilindrata is <= 0 or > 20000)
        {
            return "La cilindrata deve essere un valore in cc plausibile";
        }

        if (!Alimentazioni.IsValid(dto.Alimentazione))
        {
            return "Tipo di alimentazione non riconosciuto";
        }

        foreach (var (nome, valore) in new[]
                 {
                     ("La marca", dto.Marca),
                     ("Il modello", dto.Modello),
                     ("Il colore", dto.Colore),
                     ("Il proprietario", dto.Proprietario),
                     ("Il telefono", dto.Telefono)
                 })
        {
            if (valore?.Length > TestoMaxLength)
            {
                return $"{nome} non può superare {TestoMaxLength} caratteri";
            }
        }

        if (dto.Note?.Length > NoteMaxLength)
        {
            return $"Le note non possono superare {NoteMaxLength} caratteri";
        }

        return null;
    }
}
