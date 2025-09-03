using Microsoft.ML.Data;

namespace VirtualNanny.CryDetection.Core;

/// <summary>
/// Represents extracted audio features and label for ML training and inference.
/// </summary>
public class AudioFeatures
{
    [VectorType(13)]
    public required float[] Features { get; set; }
    public bool IsCry { get; set; }
}