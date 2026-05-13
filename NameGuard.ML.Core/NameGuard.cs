using System.Collections.Concurrent;
using Microsoft.ML;
using NameGuard.ML.Core.Heuristics;
using NameGuard.ML.Core.Models;

namespace NameGuard.ML.Core;

public sealed class NameGuard : INameGuard, IDisposable
{
    public const float DefaultThreshold = 0.5f;

    private const string EmbeddedModelResource = "NameGuard.ML.Core.Resources.model.zip";
    private static readonly char[] TokenSeparators = { ' ', '\t', '\n', '\r' };

    private readonly MLContext _mlContext;
    private readonly ITransformer _model;
    private readonly ConcurrentBag<PredictionEngine<NameInput, RawPrediction>> _pool = new();
    private readonly float _threshold;
    private int _disposed;

    public NameGuard(float threshold = DefaultThreshold)
        : this(LoadEmbeddedModel(), threshold)
    {
    }

    public NameGuard(Stream modelStream, float threshold = DefaultThreshold)
    {
        ArgumentNullException.ThrowIfNull(modelStream);
        ValidateThreshold(threshold);

        _mlContext = new MLContext(seed: 1);
        _model = _mlContext.Model.Load(modelStream, out _);
        _threshold = threshold;
    }

    public NamePrediction Check(string name)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);

        var input = (name ?? string.Empty).Trim();

        if (JunkDetector.TryReject(input, out var reason))
        {
            return new NamePrediction
            {
                IsReal = false,
                Score = 0f,
                Reason = reason,
            };
        }

        // Per-token reject: max-score aggregation below lets one strong token
        // rescue obvious junk siblings (e.g. "Khaled asd"). Catch that first.
        var separatorIdx = input.AsSpan().IndexOfAny(TokenSeparators);
        string[]? tokens = null;
        if (separatorIdx >= 0)
        {
            tokens = input.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries);
            foreach (var token in tokens)
            {
                if (JunkDetector.TryRejectToken(token, out var tokenReason))
                {
                    return new NamePrediction
                    {
                        IsReal = false,
                        Score = 0f,
                        Reason = tokenReason,
                    };
                }
            }
        }

        var score = Predict(input);

        // Multi-token aggregation: rare given/surname components can drag the
        // whole-string score down. Score each token too and take the max.
        if (tokens is not null)
        {
            foreach (var token in tokens)
            {
                if (token.Length < 2) continue;
                var tokenScore = Predict(token);
                if (tokenScore > score) score = tokenScore;
            }
        }

        var isReal = score >= _threshold;
        return new NamePrediction
        {
            IsReal = isReal,
            Score = score,
            Reason = isReal ? "ML model" : "ML model: low score",
        };
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        while (_pool.TryTake(out var engine))
        {
            engine.Dispose();
        }
    }

    private float Predict(string name)
    {
        var engine = Rent();
        try
        {
            return engine.Predict(new NameInput { Name = name }).Probability;
        }
        finally
        {
            Return(engine);
        }
    }

    private PredictionEngine<NameInput, RawPrediction> Rent()
    {
        if (_pool.TryTake(out var engine)) return engine;
        return _mlContext.Model.CreatePredictionEngine<NameInput, RawPrediction>(_model);
    }

    private void Return(PredictionEngine<NameInput, RawPrediction> engine)
    {
        if (Volatile.Read(ref _disposed) == 1)
        {
            engine.Dispose();
            return;
        }
        _pool.Add(engine);
    }

    private static void ValidateThreshold(float threshold)
    {
        if (float.IsNaN(threshold) || threshold < 0f || threshold > 1f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold),
                threshold,
                "Threshold must be a number in [0, 1].");
        }
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
