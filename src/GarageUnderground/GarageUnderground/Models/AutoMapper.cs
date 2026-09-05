namespace GarageUnderground.Models;

/// <summary>
/// Conversioni tra entità e DTO della scheda auto.
/// </summary>
public static class AutoMapper
{
    public static AutoDto ToDto(this Auto auto) => new()
    {
        Id = auto.Id,
        Targa = auto.Targa,
        Marca = auto.Marca,
        Modello = auto.Modello,
        Anno = auto.Anno,
        Colore = auto.Colore,
        Cilindrata = auto.Cilindrata,
        Alimentazione = auto.Alimentazione,
        Proprietario = auto.Proprietario,
        Telefono = auto.Telefono,
        Note = auto.Note,
        CreatedAt = auto.CreatedAt,
        UpdatedAt = auto.UpdatedAt
    };

    public static Auto ToNewEntity(this SalvaAutoDto dto) => new()
    {
        Targa = TargaNormalizer.Normalize(dto.Targa),
        Marca = Clean(dto.Marca),
        Modello = Clean(dto.Modello),
        Anno = dto.Anno,
        Colore = Clean(dto.Colore),
        Cilindrata = dto.Cilindrata,
        Alimentazione = Clean(dto.Alimentazione),
        Proprietario = Clean(dto.Proprietario),
        Telefono = Clean(dto.Telefono),
        Note = Clean(dto.Note)
    };

    public static Auto ApplyTo(this SalvaAutoDto dto, Auto existing) => existing with
    {
        Targa = TargaNormalizer.Normalize(dto.Targa),
        Marca = Clean(dto.Marca),
        Modello = Clean(dto.Modello),
        Anno = dto.Anno,
        Colore = Clean(dto.Colore),
        Cilindrata = dto.Cilindrata,
        Alimentazione = Clean(dto.Alimentazione),
        Proprietario = Clean(dto.Proprietario),
        Telefono = Clean(dto.Telefono),
        Note = Clean(dto.Note),
        UpdatedAt = DateTime.UtcNow
    };

    /// <summary>
    /// Spazi tolti; stringa vuota diventa null così i campi non compilati restano davvero assenti.
    /// </summary>
    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
