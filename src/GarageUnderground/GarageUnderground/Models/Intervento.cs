namespace GarageUnderground.Models;

/// <summary>
/// Rappresenta un intervento di manutenzione eseguito su un veicolo.
/// </summary>
public record Intervento
{
    /// <summary>
    /// Identificatore univoco dell'intervento.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Targa del veicolo su cui è stato eseguito l'intervento.
    /// </summary>
    public required string Targa { get; init; }

    /// <summary>
    /// Data in cui è stato eseguito l'intervento.
    /// </summary>
    public required DateOnly Data { get; init; }

    /// <summary>
    /// Descrizione dell'intervento eseguito.
    /// </summary>
    public required string Descrizione { get; init; }

    /// <summary>
    /// Costo dell'intervento in euro.
    /// </summary>
    public required decimal Costo { get; init; }

    /// <summary>
    /// Indica se l'intervento è già stato pagato.
    /// </summary>
    public required bool Pagato { get; init; }

    /// <summary>
    /// Data e ora di creazione del record.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
