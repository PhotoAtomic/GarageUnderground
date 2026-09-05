namespace GarageUnderground.Models;

/// <summary>
/// Riassunto degli interventi di una targa: quanti, l'ultimo, quanto speso e quanto resta da incassare.
/// </summary>
public record TargaRiepilogo(
    string Targa,
    int NumeroInterventi,
    DateOnly UltimaData,
    decimal TotaleCosto,
    decimal TotaleDaPagare);
