using Microsoft.ML.Data;

namespace NameGuard.ML.Core.Models;

public sealed class NameInput
{
    [LoadColumn(0)]
    public string Name { get; set; } = string.Empty;

    [LoadColumn(1), ColumnName("Label")]
    public bool IsReal { get; set; }
}
