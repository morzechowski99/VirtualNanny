using Microsoft.ML.Data;

namespace VirtualNanny.CryDetection.Core;

/// <summary>
/// Represents the prediction result from the cry detection model.
/// </summary>
public class CryPrediction
{
    [ColumnName("PredictedLabel")]
    public bool IsCry { get; set; }
    
    [ColumnName("Score")]
    public float[] Score { get; set; } = new float[2];
    
    /// <summary>
    /// Confidence score (0.0 to 1.0) that this is a cry.
    /// </summary>
    public float Confidence => Score?.Length > 1 ? Score[1] : 0.0f;
}