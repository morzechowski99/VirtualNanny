using Microsoft.ML.Data;

namespace VirtualNanny.CryDetection.ModelTester;

public class AudioFeatures
{
    [VectorType(13)]
    public required float[] Features { get; set; }
    public bool IsCry { get; set; }
}