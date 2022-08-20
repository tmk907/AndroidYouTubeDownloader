using Xamarin.Essentials;

namespace AndroidYouTubeDownloader.Services
{
    public class AppSettings
    {
        public static string DownloadsFolderPath
        {
            get { return Preferences.Get(nameof(DownloadsFolderPath), ""); }
            set { Preferences.Set(nameof(DownloadsFolderPath), value); }
        }
    }
}
