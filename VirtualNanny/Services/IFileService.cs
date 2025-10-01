namespace VirtualNanny.Services
{
    public interface IFileService
    {
        Task<bool> SaveFileAsync(byte[] fileData, string filename, string mimeType);
        Task<string> GetDownloadsDirectoryAsync();
    }
}