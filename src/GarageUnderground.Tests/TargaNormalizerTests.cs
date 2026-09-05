using GarageUnderground.Models;

namespace GarageUnderground.Tests;

public class TargaNormalizerTests
{
    [Theory]
    [InlineData("ab123cd", "AB123CD")]
    [InlineData("  AB 123 CD  ", "AB123CD")]
    [InlineData("AB123CD", "AB123CD")]
    public void Normalize_MaiuscolaSenzaSpazi(string input, string atteso)
    {
        Assert.Equal(atteso, TargaNormalizer.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_VuotoRestaVuoto(string? input)
    {
        Assert.Equal(string.Empty, TargaNormalizer.Normalize(input));
    }
}
