using VirtualNanny.Services;

namespace VirtualNanny.Platforms.iOS.Services
{
    public class iOSFileService : IFileService
    {
        public async Task<bool> SaveFileAsync(byte[] fileData, string filename, string mimeType)
        {
            try
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var filePath = Path.Combine(documentsPath, filename);
                
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
            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }
}