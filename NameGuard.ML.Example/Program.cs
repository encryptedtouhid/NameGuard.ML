using System.Diagnostics;
using NameGuard.ML.Core;

Console.WriteLine("NameGuard demo");
Console.WriteLine("==============");

if (args.Contains("--benchmark"))
{
    return RunBenchmark();
}

INameGuard guard;
try
{
    guard = new NameGuard.ML.Core.NameGuard();
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

if (args.Length > 0)
{
    foreach (var name in args)
    {
        var p = guard.Check(name);
        Console.WriteLine($"  {name,-30} -> {p}");
    }
    return 0;
}

var samples = new[]
{
    "John Smith",
    "Mary Johnson",
    "Khaled Hossain",
    "Yuki Tanaka",
    "Maria Garcia",
    "Jean-Luc Picard",
    "asdfgh",
    "qwerty",
    "xkqzpw",
    "aaaaaaa",
    "12345",
    "bcdfgh",
    "",
};

Console.WriteLine();
Console.WriteLine("Built-in samples:");
foreach (var name in samples)
{
    var p = guard.Check(name);
    Console.WriteLine($"  {name,-20} -> {p}");
}

Console.WriteLine();
Console.WriteLine("Enter names to check (blank line to quit):");
while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) break;
    var p = guard.Check(line);
    Console.WriteLine($"  {p}");
}
return 0;

static int RunBenchmark()
{
    Console.WriteLine();
    Console.WriteLine("Benchmark");
    Console.WriteLine("---------");

    var coldSw = Stopwatch.StartNew();
    var cold = new NameGuard.ML.Core.NameGuard();
    coldSw.Stop();
    cold.Check("warmup John Smith");
    Console.WriteLine($"  Cold start (ctor + 1 inference) : {coldSw.Elapsed.TotalMilliseconds,8:F1} ms");

    var guard = new NameGuard.ML.Core.NameGuard();
    for (var i = 0; i < 1000; i++) guard.Check("John Smith");

    var realSamples = new[]
    {
        "John Smith", "Mary Johnson", "Khaled Hossain", "Yuki Tanaka", "Maria Garcia",
        "Pierre Dubois", "Ivan Petrov", "Wei Chen", "Mohamed Ben Ali", "Joao Silva",
    };
    var fakeSamples = new[]
    {
        "asdfgh", "qwerty", "xkqzpw", "aaaaaaa", "12345", "bcdfgh", "qqqqqq", "kjhgfds",
    };

    Bench("Real names (ML path)  ", realSamples, guard, 200_000);
    Bench("Fake names (heuristic)", fakeSamples, guard, 200_000);

    var asm = typeof(NameGuard.ML.Core.NameGuard).Assembly;
    var dll = new FileInfo(asm.Location);
    Console.WriteLine();
    Console.WriteLine($"  NameGuard.ML.Core.dll size : {dll.Length / 1024.0,7:F0} KB (includes embedded model)");
    using var modelStream = asm.GetManifestResourceStream("NameGuard.ML.Core.Resources.model.zip")!;
    Console.WriteLine($"  Embedded model.zip size    : {modelStream.Length / 1024.0,7:F0} KB");

    return 0;

    static void Bench(string label, string[] samples, INameGuard g, int iterations)
    {
        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++) g.Check(samples[i % samples.Length]);
        sw.Stop();
        var perCall = sw.Elapsed.TotalMilliseconds * 1000.0 / iterations;
        var opsPerSec = iterations / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"  {label} : {perCall,8:F2} us/call  ({opsPerSec,12:N0} ops/sec)");
    }
}
