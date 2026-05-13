using NameGuard.ML.Core;

Console.WriteLine("NameGuard demo");
Console.WriteLine("==============");

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
