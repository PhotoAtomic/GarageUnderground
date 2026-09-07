using GarageUnderground.Models;
using GarageUnderground.Persistence;
using GarageUnderground.Services.Client;

namespace GarageUnderground.Services;

/// <summary>
/// Implementazione server-side del servizio auto: usa direttamente il repository
/// e applica le stesse validazioni dell'API.
/// </summary>
public sealed class ServerAutoService : IAutoService
{
    private readonly IAutoRepository repository;

    public ServerAutoService(IAutoRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<AutoDto?> GetByTargaAsync(string targa)
    {
        var auto = await repository.GetByTargaAsync(targa);
        return auto?.ToDto();
    }

    public async Task<AutoDto?> GetByIdAsync(Guid id)
    {
        var auto = await repository.GetByIdAsync(id);
        return auto?.ToDto();
    }

    public async Task<IReadOnlyList<AutoDto>> GetAllAsync()
    {
        var tutte = await repository.GetAllAsync();
        return tutte.Select(a => a.ToDto()).ToList();
    }

    public async Task<AutoDto?> CreateAsync(SalvaAutoDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (AutoValidator.Validate(dto) is not null)
        {
            return null;
        }

        var created = await repository.CreateAsync(dto.ToNewEntity());
        return created?.ToDto();
    }

    public async Task<AutoDto?> UpdateAsync(Guid id, SalvaAutoDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (AutoValidator.Validate(dto) is not null)
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
