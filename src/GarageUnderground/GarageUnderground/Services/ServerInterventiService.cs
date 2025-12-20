using GarageUnderground.Client.Models;
using GarageUnderground.Client.Services;
using GarageUnderground.Models;
using GarageUnderground.Persistence;

namespace GarageUnderground.Services;

/// <summary>
/// Implementazione server-side del servizio interventi che usa direttamente il repository.
/// Usato durante la pre-renderizzazione SSR.
/// </summary>
public sealed class ServerInterventiService : IInterventiService
{
    private readonly IInterventiRepository repository;

    public ServerInterventiService(IInterventiRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IReadOnlyList<InterventoDto>> GetByTargaAsync(string targa)
    {
        if (string.IsNullOrWhiteSpace(targa))
        {
            return [];
        }

        var interventi = await repository.GetByTargaAsync(targa);
        return interventi.Select(ToDto).ToList();
    }

    public async Task<InterventoDto?> GetByIdAsync(Guid id)
    {
        var intervento = await repository.GetByIdAsync(id);
        return intervento is null ? null : ToDto(intervento);
    }

    public async Task<InterventoDto?> CreateAsync(NuovoInterventoDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var intervento = new Intervento
        {
            Targa = dto.Targa,
            Data = dto.Data,
            Descrizione = dto.Descrizione,
            Costo = dto.Costo,
            Pagato = dto.Pagato
        };

        var created = await repository.CreateAsync(intervento);
        return ToDto(created);
    }

    public async Task<InterventoDto?> UpdateAsync(Guid id, NuovoInterventoDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var existing = await repository.GetByIdAsync(id);
        if (existing is null)
        {
            return null;
        }

        var updated = existing with
        {
            Targa = dto.Targa,
            Data = dto.Data,
            Descrizione = dto.Descrizione,
            Costo = dto.Costo,
            Pagato = dto.Pagato
        };

        var success = await repository.UpdateAsync(updated);
        return success ? ToDto(updated) : null;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await repository.DeleteAsync(id);
    }

    private static InterventoDto ToDto(Intervento intervento) => new()
    {
        Id = intervento.Id,
        Targa = intervento.Targa,
        Data = intervento.Data,
        Descrizione = intervento.Descrizione,
        Costo = intervento.Costo,
        Pagato = intervento.Pagato,
        CreatedAt = intervento.CreatedAt
    };
}
