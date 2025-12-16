namespace GarageUnderground.Models;

/// <summary>
/// DTO per la creazione di un nuovo intervento.
/// </summary>
public record NuovoInterventoRequest
{
    /// <summary>
    /// Targa del veicolo.
    /// </summary>
    public required string Targa { get; init; }

    /// <summary>
    /// Data dell'intervento.
    /// </summary>
    public required DateOnly Data { get; init; }

    /// <summary>
    /// Descrizione dell'intervento.
    /// </summary>
    public required string Descrizione { get; init; }

    /// <summary>
    /// Costo dell'intervento.
    /// </summary>
    public required decimal Costo { get; init; }

    /// <summary>
    /// Indica se l'intervento è già stato pagato.
    /// </summary>
    public required bool Pagato { get; init; }
}

/// <summary>
/// DTO per la risposta contenente un intervento.
/// </summary>
public record InterventoResponse
{
    public required Guid Id { get; init; }
    public required string Targa { get; init; }
    public required DateOnly Data { get; init; }
    public required string Descrizione { get; init; }
    public required decimal Costo { get; init; }
    public required bool Pagato { get; init; }
    public required DateTime CreatedAt { get; init; }
}
