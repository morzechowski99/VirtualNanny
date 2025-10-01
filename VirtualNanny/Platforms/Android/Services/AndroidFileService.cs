using Android.Content;
using Android.OS;
using Android.Provider;
using AndroidX.Core.Content;
using VirtualNanny.Services;
using Java.IO;
using Android.Webkit;
using Android.Media;

namespace VirtualNanny.Platforms.Android.Services
{
    public class AndroidFileService : IFileService
    {
        public async Task<bool> SaveFileAsync(byte[] fileData, string filename, string mimeType)
        {
            try
            {
                var context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
                
                global::Android.Util.Log.Info("VirtualNanny", $"Attempting to save file: {filename}, size: {fileData.Length} bytes");

                // For Android 10+ (API 29+), use MediaStore
                if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
                {
                    return await SaveFileUsingMediaStore(fileData, filename, mimeType, context);
                }
                else
                {
                    // For older Android versions, use legacy external storage
                    return await SaveFileUsingLegacyStorage(fileData, filename, context);
                }
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("VirtualNanny", $"Error saving file: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SaveFileUsingMediaStore(byte[] fileData, string filename, string mimeType, Context context)
        {
            try
            {
                var contentValues = new ContentValues();
                contentValues.Put(MediaStore.IMediaColumns.DisplayName, filename);
                contentValues.Put(MediaStore.IMediaColumns.MimeType, mimeType);
                contentValues.Put(MediaStore.IMediaColumns.RelativePath, global::Android.OS.Environment.DirectoryDownloads);

                var resolver = context.ContentResolver;
                var uri = resolver?.Insert(MediaStore.Downloads.ExternalContentUri, contentValues);

                if (uri != null && resolver != null)
                {
                    using var outputStream = resolver.OpenOutputStream(uri);
                    if (outputStream != null)
                    {
                        await outputStream.WriteAsync(fileData);
                        await outputStream.FlushAsync();
                        
                        global::Android.Util.Log.Info("VirtualNanny", $"File saved successfully using MediaStore: {filename}");
                        
                        // Notify the media scanner
                        try
                        {
                            MediaScannerConnection.ScanFile(context, new[] { uri.ToString() }, new[] { mimeType }, null);
                        }
                        catch (Exception scanEx)
                        {
                            global::Android.Util.Log.Warn("VirtualNanny", $"Media scan failed: {scanEx.Message}");
                        }
                        
                        return true;
                    }
                }
                
                global::Android.Util.Log.Warn("VirtualNanny", "Failed to create output stream for MediaStore");
                return false;
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("VirtualNanny", $"MediaStore save error: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> SaveFileUsingLegacyStorage(byte[] fileData, string filename, Context context)
        {
            try
            {
                var downloadsDir = global::Android.OS.Environment.GetExternalStoragePublicDirectory(global::Android.OS.Environment.DirectoryDownloads);
                if (downloadsDir == null)
                {
                    global::Android.Util.Log.Error("VirtualNanny", "Downloads directory not available");
                    return false;
                }

                var file = new Java.IO.File(downloadsDir, filename);
                
                using var fileOutputStream = new FileOutputStream(file);
                await fileOutputStream.WriteAsync(fileData);
                await fileOutputStream.FlushAsync();
                
                global::Android.Util.Log.Info("VirtualNanny", $"File saved successfully using legacy storage: {file.AbsolutePath}");
                
                // Notify the media scanner
                try
                {
                    MediaScannerConnection.ScanFile(context, new[] { file.AbsolutePath }, new[] { "video/*" }, null);
                }
                catch (Exception scanEx)
                {
                    global::Android.Util.Log.Warn("VirtualNanny", $"Media scan failed: {scanEx.Message}");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("VirtualNanny", $"Legacy storage save error: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetDownloadsDirectoryAsync()
        {
            await Task.CompletedTask; // Make it async for interface compliance
            
            try
            {
                var downloadsDir = global::Android.OS.Environment.GetExternalStoragePublicDirectory(global::Android.OS.Environment.DirectoryDownloads);
                return downloadsDir?.AbsolutePath ?? "/storage/emulated/0/Download";
            }
            catch
            {
                return "/storage/emulated/0/Download";
            }
        }
    }
}