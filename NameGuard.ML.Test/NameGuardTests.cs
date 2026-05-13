using NameGuard.ML.Core;
using Xunit;

namespace NameGuard.ML.Test;

public class NameGuardTests
{
    private static readonly INameGuard Guard = new NameGuard.ML.Core.NameGuard();

    [Theory]
    [InlineData("John Smith")]
    [InlineData("Mary Johnson")]
    [InlineData("Michael Brown")]
    [InlineData("Khaled Hossain")]
    [InlineData("Maria Garcia")]
    [InlineData("Yuki Tanaka")]
    [InlineData("Wei Chen")]
    [InlineData("Raj Patel")]
    public void PredictsRealForKnownLikePatterns(string name)
    {
        var p = Guard.Check(name);
        Assert.True(p.IsReal, $"Expected '{name}' to be predicted REAL (got score={p.Score:F2}, reason={p.Reason})");
    }

    [Theory]
    [InlineData("asdfgh")]
    [InlineData("qwerty")]
    [InlineData("xkqzpw")]
    [InlineData("aaaaaaa")]
    [InlineData("bcdfgh")]
    [InlineData("zzxxccvv")]
    [InlineData("12345")]
    [InlineData("")]
    [InlineData("Asd asd asd")]
    [InlineData("Jogn asd asd")]
    [InlineData("Khaled asd asd")]
    [InlineData("zephy asd")]
    [InlineData("zephy xyz")]
    public void PredictsFakeForJunk(string name)
    {
        var p = Guard.Check(name);
        Assert.False(p.IsReal, $"Expected '{name}' to be predicted FAKE (got score={p.Score:F2}, reason={p.Reason})");
    }

    [Fact]
    public void HandlesNullInput()
    {
        var p = Guard.Check(null!);
        Assert.False(p.IsReal);
    }
}
