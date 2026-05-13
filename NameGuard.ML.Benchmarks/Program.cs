using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using NameGuard.ML.Core;
using NameGuard.ML.Core.Heuristics;
using NameGuard.ML.Core.Models;

BenchmarkRunner.Run<NameGuardBenchmarks>(
    DefaultConfig.Instance.AddDiagnoser(MemoryDiagnoser.Default));

return;

[MemoryDiagnoser]
public class NameGuardBenchmarks
{
    private NameGuard.ML.Core.NameGuard _guard = null!;

    [GlobalSetup]
    public void Setup()
    {
        _guard = new NameGuard.ML.Core.NameGuard();
        // Warm up: build at least one engine in the pool.
        _guard.Check("warmup");
    }

    [GlobalCleanup]
    public void Cleanup() => _guard.Dispose();

    [Benchmark(Description = "Heuristic reject: 'asdfgh'")]
    public bool HeuristicReject() =>
        JunkDetector.TryReject("asdfgh", out _);

    [Benchmark(Description = "Heuristic pass: 'John Smith'")]
    public bool HeuristicPass() =>
        JunkDetector.TryReject("John Smith", out _);

    [Benchmark(Description = "Check: junk (heuristic path)")]
    public NamePrediction CheckJunk() => _guard.Check("asdfgh");

    [Benchmark(Description = "Check: single token (ML path)")]
    public NamePrediction CheckSingleToken() => _guard.Check("Akihito");

    [Benchmark(Description = "Check: full name (ML + token agg)")]
    public NamePrediction CheckFullName() => _guard.Check("Mary Johnson");

    [Benchmark(Description = "Check: 3-token full name")]
    public NamePrediction CheckThreeTokens() => _guard.Check("Mohamed Ben Ali");
}
