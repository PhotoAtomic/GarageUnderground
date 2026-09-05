using GarageUnderground.Models;
using LiteDB;

namespace GarageUnderground.Tests;

/// <summary>
/// Apre una COPIA del database di produzione (mai l'originale) e verifica che il codice nuovo
/// legga tutto quello che c'è e non lo alteri aggiungendo l'anagrafica.
/// Il file non è nel repository: si indica con GARAGE_PROD_DB, oppure si usa la copia in data/.
/// Senza né l'uno né l'altro i test non fanno asserzioni.
/// </summary>
public class ProductionDatabaseCompatibilityTests
{
    private static string? TrovaDatabase()
    {
        var daAmbiente = Environment.GetEnvironmentVariable("GARAGE_PROD_DB");
        if (!string.IsNullOrEmpty(daAmbiente))
        {
            // Indicato esplicitamente: se manca è un errore, non un motivo per saltare in silenzio
            if (!File.Exists(daAmbiente))
            {
                throw new FileNotFoundException(
                    $"GARAGE_PROD_DB indica un file che non esiste: {daAmbiente}", daAmbiente);
            }

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
    public void InterventiEsistenti_SonoTuttiLeggibiliConIlCodiceNuovo()
    {
        var sorgente = TrovaDatabase();
        if (sorgente is null)
        {
            return;
        }

        using var db = new TempDatabase(copyFrom: sorgente);

        // Documenti così come stanno sul disco, senza passare dal mapping delle entità
        var grezzi = db.Database.GetCollection("interventi").FindAll().ToList();
        Assert.NotEmpty(grezzi);

        var repo = db.Interventi;
        var targhe = grezzi.Select(d => d["Targa"].AsString).Distinct().ToList();

        var letti = new List<Intervento>();
        foreach (var targa in targhe)
        {
            letti.AddRange(repo.GetByTargaAsync(targa).GetAwaiter().GetResult());
        }

        // Nessun documento perso per strada: ogni riga sul disco è raggiungibile dalla sua targa
        Assert.Equal(grezzi.Count, letti.Count);

        foreach (var intervento in letti)
        {
            Assert.False(string.IsNullOrWhiteSpace(intervento.Targa));
            Assert.Equal(TargaNormalizer.Normalize(intervento.Targa), intervento.Targa);
            Assert.InRange(intervento.Data.Year, 2000, 2100);
            Assert.True(intervento.Costo >= 0);
            Assert.NotEqual(Guid.Empty, intervento.Id);
        }
    }

    [Fact]
    public void OgniCampoSopravviveAlRoundTrip()
    {
        var sorgente = TrovaDatabase();
        if (sorgente is null)
        {
            return;
        }

        using var db = new TempDatabase(copyFrom: sorgente);
        var grezzi = db.Database.GetCollection("interventi").FindAll().ToList();
        var repo = db.Interventi;

        foreach (var grezzo in grezzi)
        {
            var id = grezzo["_id"].AsGuid;
            var letto = repo.GetByIdAsync(id).GetAwaiter().GetResult();

            Assert.NotNull(letto);
            Assert.Equal(grezzo["Targa"].AsString, letto.Targa);

            // La descrizione deve arrivare identica: in produzione ce ne sono di multi-riga
            // e con accenti, ed è il campo che il meccanico legge davvero
            Assert.Equal(grezzo["Descrizione"].AsString, letto.Descrizione);
            Assert.Equal(grezzo["Costo"].AsDecimal, letto.Costo);
            Assert.Equal(grezzo["Pagato"].AsBoolean, letto.Pagato);

            var data = grezzo["Data"].AsDocument;
            Assert.Equal(
                new DateOnly(data["Year"].AsInt32, data["Month"].AsInt32, data["Day"].AsInt32),
                letto.Data);
        }
    }

    [Fact]
    public void RiepilogoTarghe_CoerenteConIDatiSulDisco()
    {
        var sorgente = TrovaDatabase();
        if (sorgente is null)
        {
            return;
        }

        using var db = new TempDatabase(copyFrom: sorgente);
        var grezzi = db.Database.GetCollection("interventi").FindAll().ToList();

        var attesoPerTarga = grezzi
            .GroupBy(d => d["Targa"].AsString)
            .ToDictionary(g => g.Key, g => (Conteggio: g.Count(), Totale: g.Sum(d => d["Costo"].AsDecimal)));

        var riepilogo = db.Interventi.GetRiepilogoTargheAsync(1000).GetAwaiter().GetResult();

        Assert.Equal(attesoPerTarga.Count, riepilogo.Count);

        foreach (var riga in riepilogo)
        {
            var atteso = attesoPerTarga[riga.Targa];
            Assert.Equal(atteso.Conteggio, riga.NumeroInterventi);
            Assert.Equal(atteso.Totale, riga.TotaleCosto);
            Assert.True(riga.TotaleDaPagare <= riga.TotaleCosto);
        }

        // Ordinamento: dalla targa vista più di recente
        Assert.Equal(riepilogo.OrderByDescending(r => r.UltimaData).Select(r => r.UltimaData), riepilogo.Select(r => r.UltimaData));
    }

    [Fact]
    public void EsportazioneCsv_ContieneTutteLeRighe()
    {
        var sorgente = TrovaDatabase();
        if (sorgente is null)
        {
            return;
        }

        using var db = new TempDatabase(copyFrom: sorgente);
        var repo = db.Interventi;
        var targa = db.Database.GetCollection("interventi").FindAll().First()["Targa"].AsString;

        var interventi = repo.GetByTargaAsync(targa).GetAwaiter().GetResult();
        var csv = InterventiCsv.Build(interventi);

        // Intestazione più un record per intervento. In produzione le descrizioni sono
        // multi-riga: gli a capo dentro le virgolette fanno parte del campo e non
        // devono contare come fine record.
        Assert.Equal(interventi.Count + 1, ContaRecord(csv));
        Assert.StartsWith("Targa;Data;Descrizione;Costo;Pagato", csv);

        foreach (var intervento in interventi)
        {
            if (intervento.Descrizione.Contains('\n'))
            {
                // Una descrizione multi-riga deve arrivare quotata, altrimenti Excel
                // la leggerebbe come righe separate
                Assert.Contains($"\"{intervento.Descrizione.Replace("\"", "\"\"")}\"", csv);
            }
        }
    }

    /// <summary>
    /// Conta i record di un CSV ignorando gli a capo che stanno dentro un campo quotato.
    /// </summary>
    private static int ContaRecord(string csv)
    {
        var record = 0;
        var inQuotes = false;
        var campoIniziato = false;

        for (var i = 0; i < csv.Length; i++)
        {
            var c = csv[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < csv.Length && csv[i + 1] == '"')
                {
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                campoIniziato = true;
            }
            else if (c == '\n' && !inQuotes)
            {
                record++;
                campoIniziato = false;
            }
            else if (c != '\r')
            {
                campoIniziato = true;
            }
        }

        return campoIniziato ? record + 1 : record;
    }

    [Fact]
    public void AggiungereUnaSchedaAuto_NonToccaGliInterventi()
    {
        var sorgente = TrovaDatabase();
        if (sorgente is null)
        {
            return;
        }

        using var db = new TempDatabase(copyFrom: sorgente);
        var interventi = db.Database.GetCollection("interventi");
        var prima = interventi.FindAll().Select(d => d.ToString()).OrderBy(s => s).ToList();
        var targa = interventi.FindAll().First()["Targa"].AsString;

        var creata = db.Auto.CreateAsync(new Auto
        {
            Targa = targa,
            Marca = "Peugeot",
            Modello = "207",
            Proprietario = "Prova"
        }).GetAwaiter().GetResult();

        Assert.NotNull(creata);

        var dopo = interventi.FindAll().Select(d => d.ToString()).OrderBy(s => s).ToList();
        Assert.Equal(prima, dopo);

        Assert.Contains("auto", db.Database.GetCollectionNames());
        Assert.Contains("interventi", db.Database.GetCollectionNames());
    }

    [Fact]
    public void UtentiERuoliEsistenti_RestanoLeggibili()
    {
        var sorgente = TrovaDatabase();
        if (sorgente is null)
        {
            return;
        }

        using var db = new TempDatabase(copyFrom: sorgente);
        var collezioni = db.Database.GetCollectionNames().ToList();

        if (collezioni.Contains("user_registrations"))
        {
            var utenti = db.Database.GetCollection<UserRegistration>("user_registrations").FindAll().ToList();
            foreach (var utente in utenti)
            {
                Assert.False(string.IsNullOrWhiteSpace(utente.Email));
            }
        }

        if (collezioni.Contains("user_roles"))
        {
            var ruoli = db.Database.GetCollection<UserRole>("user_roles").FindAll().ToList();
            foreach (var ruolo in ruoli)
            {
                Assert.False(string.IsNullOrWhiteSpace(ruolo.UserIdentifier));
                Assert.NotNull(ruolo.Roles);
            }
        }
    }
}
