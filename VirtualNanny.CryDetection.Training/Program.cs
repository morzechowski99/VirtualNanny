using Microsoft.ML;
using VirtualNanny.CryDetection.Training;

static class Program
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
        // TODO: Load data from a file or another source
        return new List<AudioData>
            {
                new AudioData { FilePath = "path/to/file1.wav", IsCry = true },
                new AudioData { FilePath = "path/to/file2.wav", IsCry = false },
                // Add more files...
            };
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
