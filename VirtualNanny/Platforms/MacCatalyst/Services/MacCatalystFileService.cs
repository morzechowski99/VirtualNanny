using VirtualNanny.Services;

namespace VirtualNanny.Platforms.MacCatalyst.Services
{
    public class MacCatalystFileService : IFileService
    {
        public async Task<bool> SaveFileAsync(byte[] fileData, string filename, string mimeType)
        {
            try
            {
                var downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                var filePath = Path.Combine(downloadsPath, filename);
                
                await File.WriteAllBytesAsync(filePath, fileData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetDownloadsDirectoryAsync()
        {
            await Task.CompletedTask;
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
    }
}