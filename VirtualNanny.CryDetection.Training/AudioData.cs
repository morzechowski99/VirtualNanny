namespace VirtualNanny.CryDetection.Training
{
    /// <summary>
    /// Represents a single audio file with its label (cry or not).
    /// </summary>
    public class AudioData
    {
        public required string FilePath { get; set; }
        public bool IsCry { get; set; }
    }
}
