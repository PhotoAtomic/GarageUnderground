namespace GarageUnderground.Models;

/// <summary>
/// Scheda auto restituita all'interfaccia e dall'API.
/// </summary>
public record AutoDto
{
    public Guid Id { get; init; }
    public string Targa { get; init; } = string.Empty;
    public string? Marca { get; init; }
    public string? Modello { get; init; }
    public int? Anno { get; init; }
    public string? Colore { get; init; }
    public int? Cilindrata { get; init; }
    public string? Alimentazione { get; init; }
    public string? Proprietario { get; init; }
    public string? Telefono { get; init; }
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// "Marca Modello" se presenti, altrimenti quello che c'è.
    /// </summary>
    public string Descrizione =>
        string.Join(' ', new[] { Marca, Modello }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

/// <summary>
/// Dati inseriti per creare o aggiornare una scheda auto.
/// </summary>
public record SalvaAutoDto
{
    public string Targa { get; init; } = string.Empty;
    public string? Marca { get; init; }
    public string? Modello { get; init; }
    public int? Anno { get; init; }
    public string? Colore { get; init; }
    public int? Cilindrata { get; init; }
    public string? Alimentazione { get; init; }
    public string? Proprietario { get; init; }
    public string? Telefono { get; init; }
    public string? Note { get; init; }
}
