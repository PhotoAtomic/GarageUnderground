namespace GarageUnderground.Models;

/// <summary>
/// Conversioni tra entità e DTO dell'intervento, in un solo posto.
/// </summary>
public static class InterventoMapper
{
    public static InterventoDto ToDto(this Intervento intervento) => new()
    {
        Id = intervento.Id,
        Targa = intervento.Targa,
        Data = intervento.Data,
        Descrizione = intervento.Descrizione,
        Costo = intervento.Costo,
        Pagato = intervento.Pagato,
        CreatedAt = intervento.CreatedAt
    };

    public static Intervento ToNewEntity(this NuovoInterventoDto dto) => new()
    {
        Targa = dto.Targa,
        Data = dto.Data,
        Descrizione = dto.Descrizione.Trim(),
        Costo = dto.Costo,
        Pagato = dto.Pagato
    };

    public static Intervento ApplyTo(this NuovoInterventoDto dto, Intervento existing) => existing with
    {
        Targa = dto.Targa,
        Data = dto.Data,
        Descrizione = dto.Descrizione.Trim(),
        Costo = dto.Costo,
        Pagato = dto.Pagato
    };
}
