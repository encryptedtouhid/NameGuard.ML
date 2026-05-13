using System.Reflection;
using Microsoft.ML;
using NameGuard.ML.Core.Heuristics;
using NameGuard.ML.Core.Models;

namespace NameGuard.ML.Core;

public sealed class NameGuard : INameGuard
{
    public const float DefaultThreshold = 0.5f;

    private const string EmbeddedModelResource = "NameGuard.ML.Core.Resources.model.zip";

    private readonly PredictionEngine<NameInput, RawPrediction> _engine;
    private readonly float _threshold;

    public NameGuard(float threshold = DefaultThreshold)
        : this(LoadEmbeddedModel(), threshold)
    {
    }

    public NameGuard(Stream modelStream, float threshold = DefaultThreshold)
    {
        if (modelStream is null) throw new ArgumentNullException(nameof(modelStream));

        var mlContext = new MLContext(seed: 1);
        var model = mlContext.Model.Load(modelStream, out _);
        _engine = mlContext.Model.CreatePredictionEngine<NameInput, RawPrediction>(model);
        _threshold = threshold;
    }

    public NamePrediction Check(string name)
    {
        var input = name ?? string.Empty;

        if (JunkDetector.TryReject(input, out var reason))
        {
            return new NamePrediction
            {
                IsReal = false,
                Score = 0f,
                Reason = reason,
            };
        }

        var raw = _engine.Predict(new NameInput { Name = input });
        var isReal = raw.Probability >= _threshold;
        return new NamePrediction
        {
            IsReal = isReal,
            Score = raw.Probability,
            Reason = isReal ? "ML model" : "ML model: low score",
        };
    }

    private static Stream LoadEmbeddedModel()
    {
        var assembly = typeof(NameGuard).Assembly;
        var stream = assembly.GetManifestResourceStream(EmbeddedModelResource);
        if (stream is null)
        {
            var available = string.Join(", ", assembly.GetManifestResourceNames());
            throw new InvalidOperationException(
                $"Embedded model '{EmbeddedModelResource}' not found. Available resources: [{available}]. " +
                $"Run the NameGuard.ML.Trainer project to produce model.zip before using NameGuard.");
        }
        return stream;
    }
}
