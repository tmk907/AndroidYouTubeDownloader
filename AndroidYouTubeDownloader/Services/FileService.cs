using Android.Content;
using Android.Media;
using Android.OS;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Essentials;
using YouTubeStreamsExtractor;
using Environment = Android.OS.Environment;

namespace AndroidYouTubeDownloader.Services
{
    public interface IFileService
    {

        string GetVideosFolderPath();
        void AddMediaFile(string path);

        string GetFolderPath();
    }

    public class FileService : IFileService
    {
        public string GetVideosFolderPath()
        {
            return Environment.GetExternalStoragePublicDirectory(Environment.DirectoryMovies).AbsolutePath;
        }

        public static string GetDownloadsFolderPath()
        {
            return Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDownloads).AbsolutePath;
        }

        public string GetFolderPath()
        {
            var dir = Path.Combine(FileService.GetDownloadsFolderPath(), "YoutubeDownloads");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            return dir;
        }

        public void AddMediaFile(string path)
        {
            MediaScannerConnection.ScanFile(Platform.CurrentActivity.ApplicationContext, new string[] { path }, null, null);

            //Intent intent = new Intent(Intent.ActionMediaScannerScanFile);
            //intent.SetData(Android.Net.Uri.FromFile(new Java.IO.File(path)));
            //CrossCurrentActivity.Current.AppContext.SendBroadcast(intent);
        }

        public static string GetExtensionFromStream(IStreamInfo stream)
        {
            if (stream is IAudioStreamInfo)
            {
                if (stream.Container == "mp4")
                {
                    return "m4a";
                }
                return stream.Container;
            }
            return stream.Container;
        }

        public static string RemoveForbiddenChars(string text)
        {
            var reservedChars = "|\\?*<\":>+[]/'";
            foreach(var ch in reservedChars)
            {
                text = text.Replace(ch.ToString(), "");
            }
            return text;
        }

        public static async Task<bool> RequestPermissions()
        {
            return await RequestStoragePermission();
        }

        private static async Task<bool> RequestStoragePermission()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();


                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                }

                if (status == PermissionStatus.Granted)
                {
                    return true;
                }
                if (status != PermissionStatus.Granted)
                {
                    //await DisplayAlert("Permission Denied", "Can not continue, try again.", "OK");
                }
            }
            catch (System.Exception ex)
            {
            }
            return false;
        }
    }
}
