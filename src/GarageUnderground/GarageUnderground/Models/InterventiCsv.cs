using System.Globalization;
using System.Text;

namespace GarageUnderground.Models;

/// <summary>
/// Esportazione CSV degli interventi, nel formato che Excel italiano apre senza importazione guidata
/// (separatore ";", date gg/mm/aaaa, decimali con la virgola).
/// </summary>
public static class InterventiCsv
{
    private const char Separator = ';';
    private static readonly CultureInfo Italiano = CultureInfo.GetCultureInfo("it-IT");

    public static string Build(IEnumerable<Intervento> interventi)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(Separator, ["Targa", "Data", "Descrizione", "Costo", "Pagato"]));

        foreach (var i in interventi)
        {
            sb.AppendLine(string.Join(Separator,
            [
                Escape(i.Targa),
                i.Data.ToString("dd/MM/yyyy", Italiano),
                Escape(i.Descrizione),
                i.Costo.ToString("0.00", Italiano),
                i.Pagato ? "Sì" : "No"
            ]));
        }

        return sb.ToString();
    }

    private static string Escape(string value)
    {
        if (value.IndexOfAny([Separator, '"', '\n', '\r']) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
