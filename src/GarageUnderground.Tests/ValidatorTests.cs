using GarageUnderground.Models;

namespace GarageUnderground.Tests;

public class InterventoValidatorTests
{
    [Fact]
    public void DatiValidi_NessunErrore()
    {
        Assert.Null(InterventoValidator.Validate("AB123CD", "Cambio olio", 50m));
    }

    [Theory]
    [InlineData("", "Cambio olio", 50, "targa")]
    [InlineData("AB123CD", "  ", 50, "descrizione")]
    [InlineData("AB123CD", "Cambio olio", -1, "costo")]
    public void DatiNonValidi_MessaggioSpecifico(string targa, string descrizione, decimal costo, string parolaAttesa)
    {
        var errore = InterventoValidator.Validate(targa, descrizione, costo);

        Assert.NotNull(errore);
        Assert.Contains(parolaAttesa, errore, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MessaggiConAccentiCorretti()
    {
        // Regressione: i sorgenti erano in Windows-1252 e "è" compilava come U+FFFD
        var errore = InterventoValidator.Validate("AB123CD", "", 0);

        Assert.NotNull(errore);
        Assert.Equal("La descrizione è obbligatoria", errore);
        Assert.DoesNotContain('�', errore);
    }
}

public class AutoValidatorTests
{
    private static SalvaAutoDto Valida() => new()
    {
        Targa = "AB123CD",
        Marca = "Fiat",
        Modello = "Panda",
        Anno = 2015,
        Cilindrata = 1200,
        Alimentazione = Alimentazioni.Benzina,
        Proprietario = "Mario Rossi"
    };

    [Fact]
    public void DatiValidi_NessunErrore()
    {
        Assert.Null(AutoValidator.Validate(Valida()));
    }

    [Fact]
    public void SoloTarga_EValida()
    {
        Assert.Null(AutoValidator.Validate(new SalvaAutoDto { Targa = "AB123CD" }));
    }

    [Fact]
    public void TargaMancante_Errore()
    {
        Assert.NotNull(AutoValidator.Validate(Valida() with { Targa = " " }));
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2500)]
    public void AnnoFuoriIntervallo_Errore(int anno)
    {
        Assert.Contains("anno", AutoValidator.Validate(Valida() with { Anno = anno })!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CilindrataNonPositiva_Errore()
    {
        Assert.Contains("cilindrata", AutoValidator.Validate(Valida() with { Cilindrata = 0 })!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AlimentazioneSconosciuta_Errore()
    {
        Assert.Contains("alimentazione", AutoValidator.Validate(Valida() with { Alimentazione = "Vapore" })!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AlimentazioneCaseInsensitive_Valida()
    {
        Assert.Null(AutoValidator.Validate(Valida() with { Alimentazione = "diesel" }));
    }
}
