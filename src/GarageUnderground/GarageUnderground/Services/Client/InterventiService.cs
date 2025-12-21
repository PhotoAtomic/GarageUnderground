using System.Net.Http.Json;
using GarageUnderground.Models;

namespace GarageUnderground.Services.Client;

/// <summary>
/// Implementazione del servizio interventi che comunica con l'API backend.
/// </summary>
public sealed class InterventiService : IInterventiService
{
    private readonly HttpClient httpClient;

    public InterventiService(HttpClient httpClient)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public async Task<IReadOnlyList<InterventoDto>> GetByTargaAsync(string targa)
    {
        if (string.IsNullOrWhiteSpace(targa))
        {
            return [];
        }

        try
        {
            var encodedTarga = Uri.EscapeDataString(targa.Trim().ToUpperInvariant());
            var response = await httpClient.GetFromJsonAsync<List<InterventoDto>>($"/api/interventi/targa/{encodedTarga}");
            return response ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }

    public async Task<InterventoDto?> GetByIdAsync(Guid id)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<InterventoDto>($"/api/interventi/{id}");
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<InterventoDto?> CreateAsync(NuovoInterventoDto intervento)
    {
        ArgumentNullException.ThrowIfNull(intervento);

        try
        {
            var response = await httpClient.PostAsJsonAsync("/api/interventi", intervento);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<InterventoDto>();
            }

            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<InterventoDto?> UpdateAsync(Guid id, NuovoInterventoDto intervento)
    {
        ArgumentNullException.ThrowIfNull(intervento);

        try
        {
            var response = await httpClient.PutAsJsonAsync($"/api/interventi/{id}", intervento);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<InterventoDto>();
            }

            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            var response = await httpClient.DeleteAsync($"/api/interventi/{id}");
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }
}
