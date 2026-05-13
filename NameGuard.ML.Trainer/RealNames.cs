using System.Text.Json;
using System.Text.Json.Serialization;

namespace NameGuard.ML.Trainer;

internal sealed class CountryNames
{
    [JsonPropertyName("g")]
    public List<string> Given { get; set; } = new();

    [JsonPropertyName("s")]
    public List<string> Surnames { get; set; } = new();
}

internal static class RealNames
{
    private const string ResourceFile = "world-names.json";

    public static Dictionary<string, CountryNames> Load(string baseDir)
    {
        var path = Path.Combine(baseDir, "Data", ResourceFile);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                $"Required dataset not found: {path}. " +
                $"Ensure Data/world-names.json is copied to output (check Trainer.csproj <None Update> entry).");
        }

        var json = File.ReadAllText(path);
        var dataset = JsonSerializer.Deserialize<Dictionary<string, CountryNames>>(json)
            ?? throw new InvalidOperationException("Failed to deserialize world-names.json");

        if (dataset.Count == 0)
            throw new InvalidOperationException("world-names.json contained no countries");

        return dataset;
    }
}
