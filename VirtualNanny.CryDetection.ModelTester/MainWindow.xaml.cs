using Microsoft.ML;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace VirtualNanny.CryDetection.ModelTester;

public partial class MainWindow : Window
{
    private readonly PredictionEngine<AudioFeatures, CryPrediction>? _predictionEngine;
    private readonly MediaPlayer _mediaPlayer = new MediaPlayer();

    public MainWindow()
    {
        InitializeComponent();
        try
        {
            var mlContext = new MLContext();
            var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "Model", "cryDetectionModel.zip");
            if (!File.Exists(modelPath))
            {
                ResultText.Text = $"Model file not found: {modelPath}";
                return;
            }
            var loadedModel = mlContext.Model.Load(modelPath, out _);
            _predictionEngine = mlContext.Model.CreatePredictionEngine<AudioFeatures, CryPrediction>(loadedModel);
        }
        catch (Exception ex)
        {
            ResultText.Text = $"Error loading model: {ex.Message}";
        }
    }

    private void SelectFile_Click(object sender, RoutedEventArgs e)
    {
        if (_predictionEngine == null)
        {
            ResultText.Text = "Model not loaded.";
            return;
        }
        var dlg = new OpenFileDialog { Filter = "WAV files (*.wav)|*.wav" };
        if (dlg.ShowDialog() != true)
            return;

        // Odtwórz plik dŸwiêkowy
        _mediaPlayer.Open(new Uri(dlg.FileName));
        _mediaPlayer.Play();

        var extractor = new MfccFeatureExtractor();
        var features = extractor.Extract(dlg.FileName);

        var input = new AudioFeatures { Features = features };
        var prediction = _predictionEngine.Predict(input);

        ResultText.Text = prediction.IsCry ? "Baby cry detected!" : "No baby cry detected.";
    }
}

public class CryPrediction
{
    [Microsoft.ML.Data.ColumnName("PredictedLabel")]
    public bool IsCry { get; set; }
}