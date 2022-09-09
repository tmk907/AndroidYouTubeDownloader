using Android.Media;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace AndroidYouTubeDownloader.Services
{
    public class FileService
    {
        public void AddMediaFile(string path)
        {
            MediaScannerConnection.ScanFile(Platform.CurrentActivity.ApplicationContext, new string[] { path }, null, null);

            //Intent intent = new Intent(Intent.ActionMediaScannerScanFile);
            //intent.SetData(Android.Net.Uri.FromFile(new Java.IO.File(path)));
            //CrossCurrentActivity.Current.AppContext.SendBroadcast(intent);
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
