namespace GarageUnderground.Models;

/// <summary>
/// Scheda anagrafica di un veicolo, identificato dalla targa.
/// Tutti i campi descrittivi sono facoltativi: il meccanico compila quello che sa.
/// </summary>
public record Auto
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Targa normalizzata (maiuscola, senza spazi). Unica.
    /// </summary>
    public required string Targa { get; init; }

    public string? Marca { get; init; }
    public string? Modello { get; init; }

    /// <summary>
    /// Anno di immatricolazione.
    /// </summary>
    public int? Anno { get; init; }

    public string? Colore { get; init; }

    /// <summary>
    /// Cilindrata in centimetri cubi.
    /// </summary>
    public int? Cilindrata { get; init; }

    /// <summary>
    /// Uno dei valori di <see cref="Alimentazioni"/>.
    /// </summary>
    public string? Alimentazione { get; init; }

    public string? Proprietario { get; init; }
    public string? Telefono { get; init; }
    public string? Note { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Tipi di alimentazione ammessi.
/// </summary>
public static class Alimentazioni
{
    public const string Benzina = "Benzina";
    public const string Diesel = "Diesel";
    public const string Gpl = "GPL";
    public const string Metano = "Metano";
    public const string Ibrida = "Ibrida";
    public const string Elettrica = "Elettrica";

    public static readonly IReadOnlyList<string> Tutte =
        [Benzina, Diesel, Gpl, Metano, Ibrida, Elettrica];

    public static bool IsValid(string? value) =>
        value is null || Tutte.Contains(value, StringComparer.OrdinalIgnoreCase);
}
