using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using AndroidX.Navigation.UI;
using AndroidX.Navigation;
using DryForest.Storage;
using Google.Android.Material.AppBar;
using Plugin.CurrentActivity;
using AndroidX.AppCompat.App;

namespace AndroidYouTubeDownloader
{
    [Activity(Label = "@string/app_name", MainLauncher = false, Exported = true)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeTypes = new[] { "text/plain", })]
    internal class ShareTargetActivity : AppCompatActivity
    {
        private NavController _navController;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            CrossCurrentActivity.Current.Init(this, savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_share_target);

            var url = GetSharedUrl(Intent);
            var bundle = new Bundle();
            bundle.PutString("video_url", url);

            _navController = Navigation.FindNavController(this, Resource.Id.myNavHostFragment);
            _navController.SetGraph(Resource.Navigation.navigation_share, bundle);

            var topAppBar = FindViewById<MaterialToolbar>(Resource.Id.topAppBar);
            topAppBar.MenuItemClick += TopAppBar_MenuItemClick;

            NavigationUI.SetupWithNavController(topAppBar, _navController);

#if DEBUG
            //HandleIntentTest();
#endif
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

        protected override void OnNewIntent(Intent? intent)
        {
            var url = GetSharedUrl(intent);
            _navController.Navigate(DownloadFragment.NavigateTo(url));
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

        private string GetSharedUrl(Intent intent)
        {
            if (Intent.ActionSend == intent?.Action && intent.Type != null)
            {
                if (intent.Type.Contains("text/plain"))
                {
                    var sharedUrl = intent.GetStringExtra(Intent.ExtraText);
                    return sharedUrl;
                }
            }
            return null;
        }

        //private async void HandleIntentTest()
        //{
        //    var url = "https://www.youtube.com/watch?v=piEyKyJ4pFg";
        //    _navController.Navigate(DownloadFragment.NavigateTo(url));
        //}

        public static void LogMessage(string text)
        {
            Log.Info("APP LOG", text);
            System.Diagnostics.Debug.WriteLine($"APP LOG: {text}");
        }
    }
}
