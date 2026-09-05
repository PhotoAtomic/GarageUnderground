namespace GarageUnderground.Models;

/// <summary>
/// Normalizzazione della targa: maiuscola, senza spazi. Usata ovunque una targa entri nel sistema,
/// così ricerche e chiavi coincidono sempre.
/// </summary>
public static class TargaNormalizer
{
    public static string Normalize(string? targa) =>
        (targa ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "");
}
