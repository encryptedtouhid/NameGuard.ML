using Microsoft.ML;
using Microsoft.ML.Data;
using NameGuard.ML.Core.Models;
using NameGuard.ML.Trainer;

const int CountPerCountry = 100;
const int Seed = 42;

var minAuc = ParseMinAuc(args);

var trainerDir = AppContext.BaseDirectory;
var projectDir = LocateProjectDir(trainerDir);
var solutionDir = Directory.GetParent(projectDir)?.FullName
    ?? throw new InvalidOperationException("Could not locate solution dir");
var coreResources = Path.Combine(solutionDir, "NameGuard.ML.Core", "Resources");
Directory.CreateDirectory(coreResources);

Console.WriteLine($"Solution dir : {solutionDir}");
Console.WriteLine($"Output target: {coreResources}/model.zip");
Console.WriteLine();

Console.WriteLine("Loading world-names.json...");
var dataset = RealNames.Load(trainerDir);
Console.WriteLine($"Countries loaded: {dataset.Count}");

var rng = new Random(Seed);
var real = DataGen.BuildReal(CountPerCountry, rng, dataset);
var fake = DataGen.BuildFake(real.Count, rng);

Console.WriteLine($"Real samples: {real.Count} ({CountPerCountry} per country)");
Console.WriteLine($"Fake samples: {fake.Count}");

var all = real.Concat(fake).ToList();
Shuffle(all, rng);

var ml = new MLContext(seed: Seed);
var data = ml.Data.LoadFromEnumerable(all);
var split = ml.Data.TrainTestSplit(data, testFraction: 0.2, seed: Seed);

Console.WriteLine();
Console.WriteLine("Training pipeline...");
var model = TrainingPipeline.Train(ml, split.TrainSet, out var pipeline);

Console.WriteLine();
Console.WriteLine("Holdout evaluation:");
var (holdoutAuc, _, _) = TrainingPipeline.EvaluateHoldout(ml, model, split.TestSet);

Console.WriteLine();
Console.WriteLine("5-fold cross-validation:");
TrainingPipeline.EvaluateCrossValidated(ml, pipeline, data);

var modelPath = Path.Combine(coreResources, "model.zip");
ml.Model.Save(model, data.Schema, modelPath);
Console.WriteLine();
Console.WriteLine($"Saved model -> {modelPath}");
Console.WriteLine($"Size: {new FileInfo(modelPath).Length / 1024.0:F1} KB");

Console.WriteLine();
Console.WriteLine("Spot checks:");
var engine = ml.Model.CreatePredictionEngine<NameInput, TrainerPrediction>(model);
foreach (var sample in new[] { "John Smith", "Mary Johnson", "Khaled Hossain", "asdfgh", "xkqzpw", "qqqqqq", "Maria Garcia", "Yuki Tanaka", "Joao Silva", "Mohamed Ben Ali" })
{
    var p = engine.Predict(new NameInput { Name = sample });
    Console.WriteLine($"  {sample,-25} -> prob={p.Probability:F3} pred={(p.PredictedLabel ? "REAL" : "FAKE")}");
}

if (minAuc is double threshold)
{
    Console.WriteLine();
    if (holdoutAuc < threshold)
    {
        Console.Error.WriteLine($"FAIL: holdout AUC {holdoutAuc:F4} < required {threshold:F4}");
        Environment.Exit(1);
    }
    Console.WriteLine($"OK: holdout AUC {holdoutAuc:F4} >= required {threshold:F4}");
}

return;

static double? ParseMinAuc(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == "--min-auc" &&
            double.TryParse(args[i + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v))
        {
            return v;
        }
    }
    return null;
}

static void Shuffle<T>(List<T> list, Random rng)
{
    for (var i = list.Count - 1; i > 0; i--)
    {
        var j = rng.Next(i + 1);
        (list[i], list[j]) = (list[j], list[i]);
    }
}

static string LocateProjectDir(string startDir)
{
    var dir = new DirectoryInfo(startDir);
    while (dir is not null)
    {
        if (dir.GetFiles("NameGuard.ML.Trainer.csproj").Length > 0)
            return dir.FullName;
        dir = dir.Parent;
    }
    throw new InvalidOperationException($"Could not locate NameGuard.ML.Trainer.csproj from {startDir}");
}

internal sealed class TrainerPrediction
{
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }

    public float Probability { get; set; }

    public float Score { get; set; }
}
