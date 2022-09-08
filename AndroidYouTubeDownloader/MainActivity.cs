using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using AndroidX.AppCompat.App;
using AndroidX.Navigation;
using AndroidX.Navigation.UI;
using DryForest.Storage;
using Google.Android.Material.AppBar;
using Plugin.CurrentActivity;

namespace AndroidYouTubeDownloader
{
    [Activity(Label = "@string/app_name", MainLauncher = true, LaunchMode = Android.Content.PM.LaunchMode.SingleInstance)]
    public class MainActivity : AppCompatActivity
    {
        private NavController _navController;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            CrossCurrentActivity.Current.Init(this, savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            _navController = Navigation.FindNavController(this, Resource.Id.myNavHostFragment);

            var topAppBar = FindViewById<MaterialToolbar>(Resource.Id.topAppBar);
            topAppBar.MenuItemClick += TopAppBar_MenuItemClick;

            NavigationUI.SetupWithNavController(topAppBar, _navController);
        }

        private void TopAppBar_MenuItemClick(object? sender, AndroidX.AppCompat.Widget.Toolbar.MenuItemClickEventArgs e)
        {
            switch (e.Item.ItemId)
            {
                case Resource.Id.settings_menu:
                    _navController.Navigate(SettingsFragment.NavigateTo);
                    break;
                default:
                    break;
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            FolderPickerHelper.HandleStorageItemPick(this, requestCode, resultCode, data);
        }

        public static void LogMessage(string text)
        {
            Log.Info("APP LOG", text);
            System.Diagnostics.Debug.WriteLine($"APP LOG: {text}");
        }
    }
}