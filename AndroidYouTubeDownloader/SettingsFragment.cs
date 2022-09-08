using Android.OS;
using AndroidX.Navigation;
using AndroidX.Preference;
using AndroidYouTubeDownloader.Services;
using DryForest.Storage;
using System;

namespace AndroidYouTubeDownloader
{
    internal class SettingsFragment : PreferenceFragmentCompat
    {
        public static INavDirections NavigateTo => new SettingsFragmentDirections();

        public override void OnCreatePreferences(Bundle? savedInstanceState, string? rootKey)
        {
            var context = PreferenceManager.Context;
            var screen = PreferenceManager.CreatePreferenceScreen(context);

            var folderCategory = new PreferenceCategory(context)
            {
                Key = "download_folder_category",
                Title = "Downloads folder"
            };
            var aboutCategory = new PreferenceCategory(context)
            {
                Key = "about_category",
                Title = "About"
            };

            screen.AddPreference(folderCategory);
            screen.AddPreference(aboutCategory);

            var downloadFolderPref = new Preference(context)
            {
                Key = "download_folder",
                Title = "Choose downloads folder",
                Summary = AppSettings.DownloadsFolderName
            };
            downloadFolderPref.PreferenceClick += DownloadFolderPref_PreferenceClick;
            folderCategory.AddPreference(downloadFolderPref);

            var versionPref = new Preference(context)
            {
                Key = "version",
                Title = "Version",
                Summary = Xamarin.Essentials.VersionTracking.CurrentVersion
            };
            aboutCategory.AddPreference(versionPref);

            PreferenceScreen = screen;
        }

        private async void DownloadFolderPref_PreferenceClick(object? sender, Preference.PreferenceClickEventArgs e)
        {
            var granted = await FileService.RequestPermissions();
            if (!granted)
            {
                return;
            }

            var folderPicker = new FolderPicker();
            try
            {
                var result = await folderPicker.PickFolderAsync();
                if (result != null)
                {
                    AppSettings.ChangeDownloadsFolder(result.Uri, result.Name);
                    FindPreference("download_folder").Summary = AppSettings.DownloadsFolderName;
                }
            }
            catch (Exception ex)
            {

            }
        }
    }

    public class SettingsFragmentDirections : Java.Lang.Object, INavDirections
    {
        public int ActionId => Resource.Id.action_global_settingsFragment;

        public Bundle Arguments => Bundle.Empty;
    }
}
