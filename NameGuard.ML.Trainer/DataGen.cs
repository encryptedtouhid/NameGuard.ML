using NameGuard.ML.Core.Models;

namespace NameGuard.ML.Trainer;

internal static class DataGen
{
    private static readonly string[] KeyboardRows =
    {
        "qwertyuiop",
        "asdfghjkl",
        "zxcvbnm",
        "1234567890",
    };

    private const string Consonants = "bcdfghjklmnpqrstvwxyz";
    private const string Letters = "abcdefghijklmnopqrstuvwxyz";

    public static List<NameInput> BuildReal(int countPerCountry, Random rng, Dictionary<string, CountryNames> dataset)
    {
        var names = new List<NameInput>(dataset.Count * countPerCountry);
        var allGivenTokens = new List<string>();
        var allSurnameTokens = new List<string>();

        foreach (var entry in dataset.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            var country = entry.Value;
            allGivenTokens.AddRange(country.Given);
            allSurnameTokens.AddRange(country.Surnames);

            var pairs = new List<(string g, string s)>(country.Given.Count * country.Surnames.Count);
            foreach (var g in country.Given)
            foreach (var s in country.Surnames)
                pairs.Add((g, s));

            Shuffle(pairs, rng);

            var take = Math.Min(countPerCountry, pairs.Count);
            for (var i = 0; i < take; i++)
            {
                var (g, s) = pairs[i];
                var roll = rng.NextDouble();
                var full = roll switch
                {
                    < 0.55 => $"{g} {s}",
                    < 0.75 => $"{g} {s}".ToLowerInvariant(),
                    < 0.85 => $"{g}-{s}",
                    < 0.95 => $"{g.ToUpperInvariant()} {s.ToUpperInvariant()}",
                    _ => $"{g} {s}",
                };
                names.Add(new NameInput { Name = full, IsReal = true });
            }
        }

        AllGivenTokens = allGivenTokens;
        AllSurnameTokens = allSurnameTokens;
        return names;
    }

    private static List<string> AllGivenTokens = new();
    private static List<string> AllSurnameTokens = new();

    public static List<NameInput> BuildFake(int target, Random rng)
    {
        var fakes = new List<NameInput>();

        while (fakes.Count < target)
        {
            var kind = rng.Next(6);
            string s = kind switch
            {
                0 => RandomLetters(rng, rng.Next(4, 12)),
                1 => KeyboardWalk(rng),
                2 => RepeatingPattern(rng),
                3 => AllConsonants(rng),
                4 => ScrambledReal(rng),
                _ => MixedJunk(rng),
            };

            if (s.Length < 2) continue;
            fakes.Add(new NameInput { Name = s, IsReal = false });
        }

        return fakes;
    }

    private static void Shuffle<T>(List<T> list, Random rng)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static string RandomLetters(Random rng, int len)
    {
        var chars = new char[len];
        for (var i = 0; i < len; i++) chars[i] = Letters[rng.Next(Letters.Length)];
        return new string(chars);
    }

    private static string KeyboardWalk(Random rng)
    {
        var row = KeyboardRows[rng.Next(KeyboardRows.Length)];
        var start = rng.Next(Math.Max(1, row.Length - 5));
        var len = rng.Next(4, Math.Min(9, row.Length - start + 1));
        return row.Substring(start, len);
    }

    private static string RepeatingPattern(Random rng)
    {
        return rng.Next(3) switch
        {
            0 => new string(Letters[rng.Next(Letters.Length)], rng.Next(4, 10)),
            1 => string.Concat(Enumerable.Repeat($"{Letters[rng.Next(Letters.Length)]}{Letters[rng.Next(Letters.Length)]}", rng.Next(3, 6))),
            _ => $"{Letters[rng.Next(Letters.Length)]}{Letters[rng.Next(Letters.Length)]}{Letters[rng.Next(Letters.Length)]}".PadRight(rng.Next(5, 10), Letters[rng.Next(Letters.Length)]),
        };
    }

    private static string AllConsonants(Random rng)
    {
        var len = rng.Next(4, 10);
        var chars = new char[len];
        for (var i = 0; i < len; i++) chars[i] = Consonants[rng.Next(Consonants.Length)];
        return new string(chars);
    }

    private static string ScrambledReal(Random rng)
    {
        var pool = rng.Next(2) == 0 ? AllGivenTokens : AllSurnameTokens;
        if (pool.Count == 0) return RandomLetters(rng, rng.Next(4, 10));
        var src = pool[rng.Next(pool.Count)];
        var arr = src.ToLowerInvariant().ToCharArray();
        for (var i = arr.Length - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
        var scrambled = new string(arr);
        return scrambled == src.ToLowerInvariant() ? scrambled + Letters[rng.Next(Letters.Length)] : scrambled;
    }

    private static string MixedJunk(Random rng)
    {
        var len = rng.Next(5, 12);
        var chars = new char[len];
        for (var i = 0; i < len; i++)
        {
            var roll = rng.Next(3);
            chars[i] = roll switch
            {
                0 => (char)('0' + rng.Next(10)),
                1 => Consonants[rng.Next(Consonants.Length)],
                _ => Letters[rng.Next(Letters.Length)],
            };
        }
        return new string(chars);
    }
}
