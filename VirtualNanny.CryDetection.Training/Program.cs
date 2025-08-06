using Microsoft.ML;

namespace VirtualNanny.CryDetection.Training;

internal static class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("VirtualNanny Cry Detection");
        Console.WriteLine("Select mode:");
        Console.WriteLine("1 - Train model");
        Console.WriteLine("2 - Test model");
        Console.Write("Choice: ");
        var choice = Console.ReadLine();

        if (choice == "2")
        {
            TestModelMenu();
        }
        else
        {
            TrainModelMenu();
        }
    }

    private static void TrainModelMenu()
    {
        var mlContext = new MLContext();
        var data = PrepareTrainingData();
        var featureData = ExtractFeatures(data);
        var trainingData = mlContext.Data.LoadFromEnumerable(featureData);
        var pipeline = mlContext.Transforms.Concatenate("Features", nameof(AudioFeatures.Features))
            .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: nameof(AudioFeatures.IsCry), featureColumnName: "Features"));
        var model = pipeline.Fit(trainingData);
        mlContext.Model.Save(model, trainingData.Schema, "Model/cryDetectionModel.zip");
        Console.WriteLine("Model training completed and saved to Model/cryDetectionModel.zip");
    }

    private static void TestModelMenu()
    {
        var data = PrepareTrainingData();
        if (data.Count == 0)
        {
            Console.WriteLine("No test data found!");
            return;
        }
        // Use all data for testing (or implement split if needed)
        var featureData = ExtractFeatures(data);
        var tester = new ModelTester("Model/cryDetectionModel.zip");
        tester.TestModel(featureData);
    }

    private static List<AudioData> PrepareTrainingData()
    {
        var data = new List<AudioData>();
        var cryFolder = Path.Combine("learning_data", "cry");
        if (Directory.Exists(cryFolder))
        {
            var cryFiles = Directory.GetFiles(cryFolder, "*.wav", SearchOption.AllDirectories);
            data.AddRange(cryFiles.Select(file => new AudioData { FilePath = file, IsCry = true }));
            Console.WriteLine($"Loaded {cryFiles.Length} cry samples from {cryFolder}");
        }
        else
        {
            Console.WriteLine($"Warning: Cry folder not found at {cryFolder}");
        }
        var noCryFolder = Path.Combine("learning_data", "no_cry");
        if (Directory.Exists(noCryFolder))
        {
            var noCryFiles = Directory.GetFiles(noCryFolder, "*.wav", SearchOption.AllDirectories);
            data.AddRange(noCryFiles.Select(file => new AudioData { FilePath = file, IsCry = false }));
            Console.WriteLine($"Loaded {noCryFiles.Length} non-cry samples from {noCryFolder}");
        }
        else
        {
            Console.WriteLine($"Warning: No-cry folder not found at {noCryFolder}");
            Console.WriteLine("Please create learning_data/no_cry folder and add non-cry audio samples");
        }
        Console.WriteLine($"Total samples loaded: {data.Count}");
        return data;
    }

    private static List<AudioFeatures> ExtractFeatures(List<AudioData> data)
    {
        var extractor = new MfccFeatureExtractor();
        var features = new List<AudioFeatures>();
        foreach (var d in data)
        {
            try
            {
                features.Add(new AudioFeatures
                {
                    Features = extractor.Extract(d.FilePath),
                    IsCry = d.IsCry
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing file {d.FilePath}: {ex.Message}");
            }
        }
        return features;
    }
}