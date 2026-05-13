using NameGuard.ML.Core;
using Xunit;

namespace NameGuard.ML.Test;

public class EdgeCaseTests
{
    private static readonly INameGuard Guard = new NameGuard.ML.Core.NameGuard();

    [Fact]
    public void TrimsWhitespace()
    {
        var p = Guard.Check("   John Smith   ");
        Assert.True(p.IsReal);
    }

    [Fact]
    public void HandlesHyphenatedNames()
    {
        var p = Guard.Check("Jean-Luc Picard");
        Assert.True(p.IsReal);
    }

    [Fact]
    public void HandlesAccents()
    {
        var p = Guard.Check("José García");
        Assert.True(p.IsReal);
    }

    [Fact]
    public void HandlesAllUppercase()
    {
        var p = Guard.Check("JOHN SMITH");
        Assert.True(p.IsReal);
    }

    [Fact]
    public void HandlesAllLowercase()
    {
        var p = Guard.Check("john smith");
        Assert.True(p.IsReal);
    }

    [Fact]
    public void ScoreInRangeZeroToOne()
    {
        var p = Guard.Check("John Smith");
        Assert.InRange(p.Score, 0f, 1f);
    }
}
