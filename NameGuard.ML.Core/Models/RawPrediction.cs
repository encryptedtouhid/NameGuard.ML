using Microsoft.ML.Data;

namespace NameGuard.ML.Core.Models;

internal sealed class RawPrediction
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }
}
