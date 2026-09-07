using System.Globalization;
using System.Text;

namespace GarageUnderground.Models;

/// <summary>
/// Su quale campo una voce dell'archivio ha risposto alla ricerca.
/// </summary>
public enum CampoCorrispondenza
{
    Nessuno,
    Targa,
    Proprietario,
    Modello
}

/// <summary>
/// Una targa dell'archivio come compare nei suggerimenti: il riepilogo degli interventi
/// (se ne ha), la scheda auto (se c'è) e il campo che ha fatto scattare la corrispondenza.
/// </summary>
public sealed record VoceArchivio(
    string Targa,
    TargaRiepilogo? Riepilogo,
    AutoDto? Auto,
    CampoCorrispondenza Corrispondenza);

/// <summary>
/// Ricerca unica su targa, proprietario e marca/modello, tutta in memoria: l'archivio di
/// un'officina è di poche centinaia di schede.
/// </summary>
public static class RicercaArchivio
{
    /// <summary>
    /// Testo confrontabile: senza accenti, maiuscolo, spazi singoli. "De Lucà" e "de luca" coincidono.
    /// </summary>
    public static string NormalizzaTesto(string? testo)
    {
        if (string.IsNullOrWhiteSpace(testo))
        {
            return string.Empty;
        }

        var decomposto = testo.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        var ultimoSpazio = true;
        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(c))
            {
                if (!ultimoSpazio)
                {
                    sb.Append(' ');
                    ultimoSpazio = true;
                }
                continue;
            }

            sb.Append(char.ToUpperInvariant(c));
            ultimoSpazio = false;
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Le voci dell'archivio che rispondono al testo, in ordine: prima le targhe che lo
    /// contengono, poi i proprietari, poi marca/modello; dentro ogni gruppo dalla più recente.
    /// Con testo vuoto restituisce tutto l'archivio dalla targa più recente.
    /// </summary>
    public static IReadOnlyList<VoceArchivio> Cerca(
        IEnumerable<TargaRiepilogo> riepilogo,
        IEnumerable<AutoDto> auto,
        string? testo)
    {
        var autoPerTarga = auto.ToDictionary(a => a.Targa, StringComparer.Ordinal);
        var riepilogoPerTarga = riepilogo.ToDictionary(r => r.Targa, StringComparer.Ordinal);
        var targhe = riepilogoPerTarga.Keys.Union(autoPerTarga.Keys, StringComparer.Ordinal);

        var targaCercata = TargaNormalizer.Normalize(testo ?? string.Empty);
        var testoCercato = NormalizzaTesto(testo);

        var voci = new List<VoceArchivio>();
        foreach (var targa in targhe)
        {
            riepilogoPerTarga.TryGetValue(targa, out var r);
            autoPerTarga.TryGetValue(targa, out var a);

            var campo = CampoCorrispondenza.Nessuno;
            if (testoCercato.Length > 0)
            {
                if (targaCercata.Length > 0 && targa.Contains(targaCercata, StringComparison.Ordinal))
                {
                    campo = CampoCorrispondenza.Targa;
                }
                else if (NormalizzaTesto(a?.Proprietario).Contains(testoCercato, StringComparison.Ordinal))
                {
                    campo = CampoCorrispondenza.Proprietario;
                }
                else if (NormalizzaTesto(a?.Descrizione).Contains(testoCercato, StringComparison.Ordinal))
                {
                    campo = CampoCorrispondenza.Modello;
                }
                else
                {
                    continue;
                }
            }

            voci.Add(new VoceArchivio(targa, r, a, campo));
        }

        return voci
            .OrderBy(v => v.Corrispondenza)
            .ThenByDescending(v => v.Riepilogo?.UltimaData ?? DateOnly.MinValue)
            .ThenBy(v => v.Targa, StringComparer.Ordinal)
            .ToList();
    }
}
