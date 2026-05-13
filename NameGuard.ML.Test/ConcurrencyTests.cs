using NameGuard.ML.Core;
using Xunit;

namespace NameGuard.ML.Test;

public class ConcurrencyTests
{
    [Fact]
    public async Task Check_IsThreadSafe_UnderParallelLoad()
    {
        using var guard = new NameGuard.ML.Core.NameGuard();

        var samples = new[]
        {
            "John Smith", "Mary Johnson", "Khaled Hossain", "Yuki Tanaka",
            "asdfgh", "qwerty", "xkqzpw", "aaaaaa",
            "Maria Garcia", "Wei Chen", "Raj Patel", "Jean-Luc Picard",
        };

        const int iterations = 200;
        const int parallelism = 16;

        var tasks = Enumerable.Range(0, parallelism).Select(workerId => Task.Run(() =>
        {
            var rng = new Random(workerId);
            for (var i = 0; i < iterations; i++)
            {
                var sample = samples[rng.Next(samples.Length)];
                var p = guard.Check(sample);
                Assert.InRange(p.Score, 0f, 1f);
                Assert.NotNull(p.Reason);
            }
        })).ToArray();

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task Check_ProducesStableScores_UnderConcurrency()
    {
        using var guard = new NameGuard.ML.Core.NameGuard();
        const string sample = "Mary Johnson";

        var baseline = guard.Check(sample).Score;

        var tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            for (var i = 0; i < 50; i++)
            {
                var p = guard.Check(sample);
                Assert.Equal(baseline, p.Score);
            }
        })).ToArray();

        await Task.WhenAll(tasks);
    }
}
