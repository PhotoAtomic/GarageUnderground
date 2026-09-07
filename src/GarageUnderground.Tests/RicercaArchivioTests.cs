using GarageUnderground.Models;

namespace GarageUnderground.Tests;

public class RicercaArchivioTests
{
    private static readonly TargaRiepilogo Panda = new("AB123CD", 3, new DateOnly(2026, 8, 1), 300m, 100m);
    private static readonly TargaRiepilogo Captiva = new("EF456GH", 1, new DateOnly(2026, 9, 1), 500m, 0m);
    private static readonly TargaRiepilogo SenzaScheda = new("ZZ999ZZ", 2, new DateOnly(2026, 7, 1), 50m, 50m);

    private static readonly AutoDto PandaAuto = new()
    {
        Targa = "AB123CD", Marca = "Fiat", Modello = "Panda", Cilindrata = 1200, Proprietario = "Mario Rossi"
    };

    private static readonly AutoDto CaptivaAuto = new()
    {
        Targa = "EF456GH", Marca = "Chevrolet", Modello = "Captiva", Proprietario = "Anna De Lucà"
    };

    private static readonly AutoDto SoloScheda = new()
    {
        Targa = "QQ000QQ", Marca = "Fiat", Modello = "500", Proprietario = "Rossini Luca"
    };

    private static IReadOnlyList<VoceArchivio> Cerca(string? testo) =>
        RicercaArchivio.Cerca([Panda, Captiva, SenzaScheda], [PandaAuto, CaptivaAuto, SoloScheda], testo);

    [Fact]
    public void TestoVuoto_TuttoDallaPiuRecente_SchedeSenzaInterventiInFondo()
    {
        var voci = Cerca("");

        Assert.Equal(["EF456GH", "AB123CD", "ZZ999ZZ", "QQ000QQ"], voci.Select(v => v.Targa));
        Assert.All(voci, v => Assert.Equal(CampoCorrispondenza.Nessuno, v.Corrispondenza));
    }

    [Fact]
    public void Targa_ParzialeEConSpazi_TrovaPerTarga()
    {
        var voci = Cerca("ab 123");

        var unica = Assert.Single(voci);
        Assert.Equal("AB123CD", unica.Targa);
        Assert.Equal(CampoCorrispondenza.Targa, unica.Corrispondenza);
        Assert.NotNull(unica.Auto);
        Assert.NotNull(unica.Riepilogo);
    }

    [Fact]
    public void Proprietario_SenzaAccentiEMaiuscole_TrovaAncheSchedeSenzaInterventi()
    {
        var voci = Cerca("de luca");

        var unica = Assert.Single(voci);
        Assert.Equal("EF456GH", unica.Targa);
        Assert.Equal(CampoCorrispondenza.Proprietario, unica.Corrispondenza);

        var ross = Cerca("ross");
        Assert.Equal(["AB123CD", "QQ000QQ"], ross.Select(v => v.Targa));
        Assert.Null(ross[1].Riepilogo);
    }

    [Fact]
    public void Modello_TrovaPerMarcaOModello_DopoTargheEProprietari()
    {
        var voci = Cerca("fiat");

        Assert.Equal(["AB123CD", "QQ000QQ"], voci.Select(v => v.Targa));
        Assert.All(voci, v => Assert.Equal(CampoCorrispondenza.Modello, v.Corrispondenza));
    }

    [Fact]
    public void Ordine_TargaPrimaDiProprietarioPrimaDiModello()
    {
        // "CA" sta nella targa AB123CD? no. Sta in "Captiva" (modello) e in "De Lucà" (proprietario)
        var voci = Cerca("ca");

        Assert.Equal(CampoCorrispondenza.Proprietario, voci[0].Corrispondenza);
        Assert.Equal("EF456GH", voci[0].Targa);
    }

    [Fact]
    public void NormalizzaTesto_TogliAccentiSpaziDoppiEMaiuscole()
    {
        Assert.Equal("DE LUCA MARIA", RicercaArchivio.NormalizzaTesto("  dé   Lucà  maría "));
        Assert.Equal("", RicercaArchivio.NormalizzaTesto(null));
    }
}
