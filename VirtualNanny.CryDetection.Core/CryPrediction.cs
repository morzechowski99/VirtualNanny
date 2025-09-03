using Microsoft.ML.Data;

namespace VirtualNanny.CryDetection.Core;

/// <summary>
/// Represents the prediction result from the cry detection model.
/// </summary>
public class CryPrediction
{
    [ColumnName("PredictedLabel")]
    public bool IsCry { get; set; }

    [ColumnName("Probability")]
    public float Probability { get; set; }

    /// <summary>
    /// Confidence score (0.0 to 1.0) that this is a cry.
    /// </summary>
    public float Confidence => IsCry ? Probability : (1.0f - Probability);
}