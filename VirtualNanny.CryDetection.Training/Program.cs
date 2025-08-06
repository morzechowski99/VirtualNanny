using Microsoft.ML;

namespace VirtualNanny.CryDetection.Training;

internal static class Program
{
    private static void Main(string[] args)
    {
        var mlContext = new MLContext();
        var data = PrepareTrainingData();
        var featureData = ExtractFeatures(data);
        var trainingData = mlContext.Data.LoadFromEnumerable(featureData);
        var pipeline = mlContext.Transforms.Concatenate("Features", nameof(AudioFeatures.Features))
            .Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(labelColumnName: nameof(AudioFeatures.IsCry), featureColumnName: "Features"));
        var model = pipeline.Fit(trainingData);
        mlContext.Model.Save(model, trainingData.Schema, "cryDetectionModel.zip");
    }

    private static List<AudioData> PrepareTrainingData()
    {
        var data = new List<AudioData>();
        
        // Load cry samples from learning_data/cry folder
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

        // Load non-cry samples from learning_data/no_cry folder
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