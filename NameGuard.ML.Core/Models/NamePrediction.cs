namespace NameGuard.ML.Core.Models;

public sealed class NamePrediction
{
    public bool IsReal { get; init; }
    public float Score { get; init; }
    public string Reason { get; init; } = string.Empty;

    public override string ToString() =>
        $"{(IsReal ? "REAL" : "FAKE")} (score={Score:F2}) — {Reason}";
}
