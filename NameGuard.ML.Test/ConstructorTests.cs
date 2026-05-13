using NameGuard.ML.Core;
using Xunit;

namespace NameGuard.ML.Test;

public class ConstructorTests
{
    [Fact]
    public void EmbeddedCtor_DefaultThreshold_Works()
    {
        using var guard = new NameGuard.ML.Core.NameGuard();
        var p = guard.Check("John Smith");
        Assert.True(p.IsReal);
    }

    [Fact]
    public void StreamCtor_LoadsModelFromStream()
    {
        var assembly = typeof(NameGuard.ML.Core.NameGuard).Assembly;
        using var modelStream = assembly.GetManifestResourceStream("NameGuard.ML.Core.Resources.model.zip");
        Assert.NotNull(modelStream);

        using var guard = new NameGuard.ML.Core.NameGuard(modelStream!);
        var p = guard.Check("Mary Johnson");
        Assert.True(p.IsReal);
    }

    [Fact]
    public void StreamCtor_NullStream_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new NameGuard.ML.Core.NameGuard((Stream)null!));
    }

    [Theory]
    [InlineData(-0.0001f)]
    [InlineData(1.0001f)]
    [InlineData(float.NaN)]
    [InlineData(float.NegativeInfinity)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(-1f)]
    [InlineData(2f)]
    public void Ctor_InvalidThreshold_Throws(float threshold)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NameGuard.ML.Core.NameGuard(threshold));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(0.5f)]
    [InlineData(0.7f)]
    [InlineData(1f)]
    public void Ctor_ValidThreshold_Works(float threshold)
    {
        using var guard = new NameGuard.ML.Core.NameGuard(threshold);
        var p = guard.Check("John Smith");
        Assert.InRange(p.Score, 0f, 1f);
    }

    [Fact]
    public void Threshold_AtOne_RejectsBorderline()
    {
        using var strict = new NameGuard.ML.Core.NameGuard(threshold: 1f);
        // Any score < 1 should classify as not-real with threshold = 1.
        var p = strict.Check("Akihito");
        Assert.False(p.IsReal);
    }

    [Fact]
    public void Threshold_AtZero_AcceptsAllNonHeuristicReject()
    {
        using var permissive = new NameGuard.ML.Core.NameGuard(threshold: 0f);
        var p = permissive.Check("John Smith");
        Assert.True(p.IsReal);
    }

    [Fact]
    public void Dispose_PreventsFurtherUse()
    {
        var guard = new NameGuard.ML.Core.NameGuard();
        guard.Check("John Smith"); // warm pool
        guard.Dispose();
        Assert.Throws<ObjectDisposedException>(() => guard.Check("John Smith"));
    }
}
