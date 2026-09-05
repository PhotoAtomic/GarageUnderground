using GarageUnderground.Models;
using GarageUnderground.Services.Client;
using GarageUnderground.Persistence;

namespace GarageUnderground.Services;

/// <summary>
/// Implementazione server-side del servizio interventi che usa direttamente il repository.
/// Applica le stesse validazioni dell'API: è il percorso usato dalle pagine Blazor.
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
        return interventi.Select(i => i.ToDto()).ToList();
    }

    public Task<IReadOnlyList<TargaRiepilogo>> GetRiepilogoTargheAsync(int limit = 50)
    {
        return repository.GetRiepilogoTargheAsync(limit);
    }

    public async Task<InterventoDto?> SetPagatoAsync(Guid id, bool pagato)
    {
        var existing = await repository.GetByIdAsync(id);
        if (existing is null)
        {
            return null;
        }

        var updated = existing with { Pagato = pagato };
        var success = await repository.UpdateAsync(updated);
        return success ? updated.ToDto() : null;
    }

    public async Task<InterventoDto?> GetByIdAsync(Guid id)
    {
        var intervento = await repository.GetByIdAsync(id);
        return intervento?.ToDto();
    }

    public async Task<InterventoDto?> CreateAsync(NuovoInterventoDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (InterventoValidator.Validate(dto) is not null)
        {
            return null;
        }

        var created = await repository.CreateAsync(dto.ToNewEntity());
        return created.ToDto();
    }

    public async Task<InterventoDto?> UpdateAsync(Guid id, NuovoInterventoDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (InterventoValidator.Validate(dto) is not null)
        {
            return null;
        }

        var existing = await repository.GetByIdAsync(id);
        if (existing is null)
        {
            return null;
        }

        var updated = dto.ApplyTo(existing);
        var success = await repository.UpdateAsync(updated);
        return success ? updated.ToDto() : null;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await repository.DeleteAsync(id);
    }
}
