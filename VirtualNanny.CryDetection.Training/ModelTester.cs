using Microsoft.ML;

namespace VirtualNanny.CryDetection.Training;

public class ModelTester
{
    private readonly MLContext _mlContext;
    private readonly string _modelPath;

    public ModelTester(string modelPath)
    {
        _mlContext = new MLContext();
        _modelPath = modelPath;
    }

    public void TestModel(IEnumerable<AudioFeatures> testData)
    {
        if (!File.Exists(_modelPath))
        {
            Console.WriteLine($"Model file not found: {_modelPath}");
            return;
        }

        var dataView = _mlContext.Data.LoadFromEnumerable(testData);
        var loadedModel = _mlContext.Model.Load(_modelPath, out _);
        var predictions = loadedModel.Transform(dataView);
        var metrics = _mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: nameof(AudioFeatures.IsCry));

        Console.WriteLine("Test results:");
        Console.WriteLine($"  Accuracy: {metrics.Accuracy:P2}");
        Console.WriteLine($"  AUC: {metrics.AreaUnderRocCurve:P2}");
        Console.WriteLine($"  F1 Score: {metrics.F1Score:P2}");
    }
}
