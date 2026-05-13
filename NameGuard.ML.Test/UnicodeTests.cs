using NameGuard.ML.Core;
using NameGuard.ML.Core.Heuristics;
using Xunit;

namespace NameGuard.ML.Test;

public class UnicodeTests
{
    private static readonly INameGuard Guard = new NameGuard.ML.Core.NameGuard();

    [Theory]
    [InlineData("Иван Петров")]      // Cyrillic
    [InlineData("田中 太郎")]            // CJK
    [InlineData("محمد")]               // Arabic
    [InlineData("Δημήτριος")]          // Greek
    [InlineData("שלום")]               // Hebrew
    public void RejectsNonLatinScript_InHeuristics(string input)
    {
        var rejected = JunkDetector.TryReject(input, out var reason);
        Assert.True(rejected, $"Expected '{input}' to be rejected");
        Assert.Equal("Non-Latin script", reason);
    }

    [Theory]
    [InlineData("Иван Петров")]
    [InlineData("田中 太郎")]
    [InlineData("محمد")]
    public void RejectsNonLatinScript_ViaCheck(string input)
    {
        var p = Guard.Check(input);
        Assert.False(p.IsReal);
        Assert.Equal("Non-Latin script", p.Reason);
        Assert.Equal(0f, p.Score);
    }

    [Theory]
    [InlineData("José García")]        // Spanish
    [InlineData("Łukasz Kowalski")]    // Polish
    [InlineData("Søren Hansen")]       // Danish
    [InlineData("François")]           // French
    [InlineData("Ångström")]           // Swedish
    [InlineData("Đức Nguyễn")]         // Vietnamese (Latin Extended Additional)
    public void AcceptsLatinExtended(string input)
    {
        var rejected = JunkDetector.TryReject(input, out var reason);
        Assert.False(rejected, $"Expected '{input}' to pass heuristics (reason was '{reason}')");
    }
}
