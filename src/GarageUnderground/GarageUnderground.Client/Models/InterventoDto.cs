namespace GarageUnderground.Client.Models;

/// <summary>
/// DTO per la risposta contenente un intervento.
/// </summary>
public record InterventoDto
{
    public Guid Id { get; init; }
    public string Targa { get; init; } = string.Empty;
    public DateOnly Data { get; init; }
    public string Descrizione { get; init; } = string.Empty;
    public decimal Costo { get; init; }
    public bool Pagato { get; init; }
    public DateTime CreatedAt { get; init; }
}

/// <summary>
/// DTO per la creazione di un nuovo intervento.
/// </summary>
public record NuovoInterventoDto
{
    public string Targa { get; init; } = string.Empty;
    public DateOnly Data { get; init; }
    public string Descrizione { get; init; } = string.Empty;
    public decimal Costo { get; init; }
    public bool Pagato { get; init; }
}
