using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using NameGuard.ML.Core.Models;

namespace NameGuard.ML.Trainer;

internal static class TrainingPipeline
{
    public static ITransformer Train(MLContext ml, IDataView trainingData, out IEstimator<ITransformer> pipeline)
    {
        pipeline = ml.Transforms.Text.NormalizeText(
                outputColumnName: "NormText",
                inputColumnName: nameof(NameInput.Name),
                caseMode: TextNormalizingEstimator.CaseMode.Lower,
                keepDiacritics: false,
                keepPunctuations: false,
                keepNumbers: true)
            .Append(ml.Transforms.Text.TokenizeIntoCharactersAsKeys(
                outputColumnName: "Chars",
                inputColumnName: "NormText",
                useMarkerCharacters: true))
            .Append(ml.Transforms.Text.ProduceNgrams(
                outputColumnName: "Features",
                inputColumnName: "Chars",
                ngramLength: 4,
                useAllLengths: true,
                weighting: NgramExtractingEstimator.WeightingCriteria.TfIdf))
            .Append(ml.BinaryClassification.Trainers.FastTree(
                labelColumnName: "Label",
                featureColumnName: "Features",
                numberOfLeaves: 32,
                numberOfTrees: 150,
                minimumExampleCountPerLeaf: 5,
                learningRate: 0.1));

        return pipeline.Fit(trainingData);
    }

    public static void EvaluateCrossValidated(MLContext ml, IEstimator<ITransformer> pipeline, IDataView data)
    {
        var cv = ml.BinaryClassification.CrossValidate(data, pipeline, numberOfFolds: 5, labelColumnName: "Label");
        var avgAuc = cv.Average(r => r.Metrics.AreaUnderRocCurve);
        var avgAcc = cv.Average(r => r.Metrics.Accuracy);
        var avgF1 = cv.Average(r => r.Metrics.F1Score);
        Console.WriteLine($"  CV AUC      : {avgAuc:F4}");
        Console.WriteLine($"  CV Accuracy : {avgAcc:F4}");
        Console.WriteLine($"  CV F1       : {avgF1:F4}");
    }

    public static void EvaluateHoldout(MLContext ml, ITransformer model, IDataView test)
    {
        var preds = model.Transform(test);
        var metrics = ml.BinaryClassification.Evaluate(preds, labelColumnName: "Label");
        Console.WriteLine($"  Holdout AUC      : {metrics.AreaUnderRocCurve:F4}");
        Console.WriteLine($"  Holdout Accuracy : {metrics.Accuracy:F4}");
        Console.WriteLine($"  Holdout F1       : {metrics.F1Score:F4}");
    }
}
