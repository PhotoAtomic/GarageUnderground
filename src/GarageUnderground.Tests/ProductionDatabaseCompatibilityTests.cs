using GarageUnderground.Models;
using LiteDB;

namespace GarageUnderground.Tests;

/// <summary>
/// Apre una COPIA del database di produzione (mai l'originale) e verifica che il codice nuovo
/// legga tutto quello che c'è e non lo alteri aggiungendo l'anagrafica.
/// Il file non è nel repository: si cerca in GARAGE_PROD_DB oppure nella cartella data/ del progetto.
/// Se non c'è, il test non fa asserzioni.
/// </summary>
public class ProductionDatabaseCompatibilityTests
{
    private static string? TrovaDatabase()
    {
        var daAmbiente = Environment.GetEnvironmentVariable("GARAGE_PROD_DB");
        if (!string.IsNullOrEmpty(daAmbiente) && File.Exists(daAmbiente))
        {
            return daAmbiente;
        }

        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8 && dir is not null; i++)
        {
            var candidato = Path.Combine(dir, "GarageUnderground", "GarageUnderground", "data", "garageunderground.db");
            if (File.Exists(candidato))
            {
                return candidato;
            }

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }

    [Fact]
    public async Task InterventiEsistenti_SonoLeggibiliConIlCodiceNuovo()
    {
        var sorgente = TrovaDatabase();
        if (sorgente is null)
        {
            return;
        }

        using var db = new TempDatabase(copyFrom: sorgente);

        var grezzi = db.Database.GetCollection("interventi").FindAll().ToList();
        Assert.NotEmpty(grezzi);

        var repo = db.Interventi;
        var targhe = grezzi.Select(d => d["Targa"].AsString).Distinct().ToList();

        var totaleLetti = 0;
        foreach (var targa in targhe)
        {
            var letti = await repo.GetByTargaAsync(targa);
            totaleLetti += letti.Count;

            foreach (var intervento in letti)
            {
                Assert.Equal(targa, intervento.Targa);
                Assert.InRange(intervento.Data.Year, 2000, 2100);
                Assert.False(string.IsNullOrEmpty(intervento.Descrizione));
                Assert.True(intervento.Costo >= 0);
            }
        }

        Assert.Equal(grezzi.Count, totaleLetti);
    }

    [Fact]
    public async Task AggiungereUnaSchedaAuto_NonToccaGliInterventi()
    {
        var sorgente = TrovaDatabase();
        if (sorgente is null)
        {
            return;
        }

        using var db = new TempDatabase(copyFrom: sorgente);
        var interventi = db.Database.GetCollection("interventi");
        var prima = interventi.FindAll().Select(d => d.ToString()).OrderBy(s => s).ToList();
        var targa = prima.Count > 0 ? interventi.FindAll().First()["Targa"].AsString : "AA000AA";

        var creata = await db.Auto.CreateAsync(new Auto { Targa = targa, Marca = "Test" });
        Assert.NotNull(creata);

        var dopo = interventi.FindAll().Select(d => d.ToString()).OrderBy(s => s).ToList();
        Assert.Equal(prima, dopo);

        Assert.Contains("auto", db.Database.GetCollectionNames());
        Assert.Contains("interventi", db.Database.GetCollectionNames());
    }
}
