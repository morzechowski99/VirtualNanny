using Microsoft.ML;
using VirtualNanny.AudioAnalysis.Interfaces;
using VirtualNanny.CryDetection.Core;

namespace VirtualNanny.AudioAnalysis;

public class MlNetCryAnalyzer : ICryDetector
{
    private readonly PredictionEngine<AudioFeatures, CryPrediction> _predictionEngine;
    private readonly MfccFeatureExtractor _featureExtractor;
    private bool _disposed;

    public MlNetCryAnalyzer(string? modelPath = null)
    {
        var modelPath1 = modelPath ?? Path.Combine("Model", "cryDetectionModel.zip");
        
        if (!File.Exists(modelPath1))
        {
            throw new FileNotFoundException($"Model file not found at: {modelPath1}. Please train the model first using VirtualNanny.CryDetection.Training.");
        }

        var mlContext = new MLContext();
        var model = mlContext.Model.Load(modelPath1, out _);
        _predictionEngine = mlContext.Model.CreatePredictionEngine<AudioFeatures, CryPrediction>(model);
        _featureExtractor = new MfccFeatureExtractor();
    }

    public bool IsCryDetected(short[] audioSamples, short threshold = 10000)
    {
        try
        {
            // Convert short array to float array and normalize
            var floatSamples = audioSamples.Select(s => s / 32768.0f).ToArray();
            
            // Extract MFCC features directly from the audio samples - no temporary files needed!
            var features = _featureExtractor.ExtractFromSamples(floatSamples);
            
            var audioFeatures = new AudioFeatures
            {
                Features = features
            };

            // Get prediction from ML.NET model
            var prediction = _predictionEngine.Predict(audioFeatures);
            
            // You can adjust this threshold based on your requirements
            // The threshold parameter is not used in ML.NET approach, but we keep it for interface compatibility
            return prediction.IsCry && prediction.Confidence > 0.5f;
        }
        catch (Exception ex)
        {
            // Log the exception in a real application
            Console.WriteLine($"Error during cry detection: {ex.Message}");
            return false;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            _predictionEngine?.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
