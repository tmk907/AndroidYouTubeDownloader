using Xamarin.Essentials;

namespace AndroidYouTubeDownloader.Services
{
    public class AppSettings
    {
        public static string DownloadsFolderPath
        {
            get { return Preferences.Get(nameof(DownloadsFolderPath), ""); }
            private set { Preferences.Set(nameof(DownloadsFolderPath), value); }
        }

        public static string DownloadsFolderName
        {
            get { return Preferences.Get(nameof(DownloadsFolderName), ""); }
            private set { Preferences.Set(nameof(DownloadsFolderName), value); }
        }

        public static void ChangeDownloadsFolder(string path, string name)
        {
            DownloadsFolderPath = path;
            DownloadsFolderName = name;
        }
    }
}
