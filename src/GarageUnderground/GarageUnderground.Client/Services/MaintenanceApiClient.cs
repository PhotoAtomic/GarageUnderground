using System.Net.Http.Json;
using System.Text.Json;
using GarageUnderground.Shared.Json;
using GarageUnderground.Shared.Models;
using LiteDB;

namespace GarageUnderground.Client.Services;

public class MaintenanceApiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();
    private readonly HttpClient httpClient;

    public MaintenanceApiClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<IReadOnlyList<MaintenanceRecord>> GetAsync(string? licensePlate, string? query, CancellationToken cancellationToken = default)
    {
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(licensePlate))
        {
            parameters.Add($"licensePlate={Uri.EscapeDataString(licensePlate)}");
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            parameters.Add($"query={Uri.EscapeDataString(query)}");
        }

        var uri = "/api/maintenance";
        if (parameters.Any())
        {
            uri = $"{uri}?{string.Join("&", parameters)}";
        }

        var response = await httpClient.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();

        var records = await response.Content.ReadFromJsonAsync<List<MaintenanceRecord>>(SerializerOptions, cancellationToken);
        return records ?? [];
    }

    public async Task<MaintenanceRecord?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"/api/maintenance/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MaintenanceRecord>(SerializerOptions, cancellationToken);
    }

    public async Task<MaintenanceRecord> SaveAsync(MaintenanceRecord record, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;

        if (record.Id == ObjectId.Empty)
        {
            response = await httpClient.PostAsJsonAsync("/api/maintenance", record, SerializerOptions, cancellationToken);
        }
        else
        {
            response = await httpClient.PutAsJsonAsync($"/api/maintenance/{record.Id}", record, SerializerOptions, cancellationToken);
        }

        response.EnsureSuccessStatusCode();
        var saved = await response.Content.ReadFromJsonAsync<MaintenanceRecord>(SerializerOptions, cancellationToken);
        return saved ?? record;
    }

    public async Task DeleteAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/maintenance/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new ObjectIdJsonConverter());
        return options;
    }
}
