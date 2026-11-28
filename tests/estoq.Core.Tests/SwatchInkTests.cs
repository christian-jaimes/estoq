using Estoq.Core.Inventory;
using Xunit;

namespace Estoq.Core.Tests;

public class SwatchInkTests
{
    [Theory]
    [InlineData("#FFFFFF", true)]
    [InlineData("#FAF9F6", true)]
    [InlineData("#E2C995", true)]
    [InlineData("#000000", false)]
    [InlineData("#1F3A5F", false)]
    [InlineData("#B22C2C", false)]
    public void DarkTextOnLightColoursAndLightTextOnDarkOnes(string hex, bool expectedDark) =>
        Assert.Equal(expectedDark, SwatchInk.UseDarkText(hex));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("red")]
    [InlineData("#12")]
    public void UnreadableColourUsesDarkText(string? hex) => Assert.True(SwatchInk.UseDarkText(hex));
}
