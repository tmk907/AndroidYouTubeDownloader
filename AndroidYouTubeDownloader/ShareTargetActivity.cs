using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Webkit;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.RecyclerView.Widget;
using AndroidYouTubeDownloader.Services;
using AndroidYouTubeDownloader.ViewModels;
using Bumptech.Glide;
using DryForest.Storage;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.Snackbar;
using Google.Android.Material.Tabs;
using Plugin.CurrentActivity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace AndroidYouTubeDownloader
{
    [Activity(Label = "@string/app_name", MainLauncher = false, Exported = true)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeTypes = new[] { "text/plain", })]
    internal class ShareTargetActivity : Activity
    {
        private DownloadItemsAdapter _downloadItemsAdapter;
        private CircularProgressIndicator _loadingVideoProgressRing;
        private ConstraintLayout _container;
        private TabLayout _tabLayout;
        private LinearProgressIndicator _downloadProgressBar;
        private WebView _webView;

        private VideoDataVM VideoDataVM;

        private YouTubeService _youtubeService;
        private DownloadService _downloadService;
        private WebViewJsEngine _jsEngine;
        private bool _isDownloading;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            CrossCurrentActivity.Current.Init(this, savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_share_target);

            _loadingVideoProgressRing = FindViewById<CircularProgressIndicator>(Resource.Id.loadingVideoProgressRing);
            _downloadProgressBar = FindViewById<LinearProgressIndicator>(Resource.Id.dowloadProgressBar);
            _container = FindViewById<ConstraintLayout>(Resource.Id.container);
            _tabLayout = FindViewById<TabLayout>(Resource.Id.tabLayout1);
            _tabLayout.TabSelected += OnTabSelected;

            var mRecyclerView = FindViewById<RecyclerView>(Resource.Id.recyclerView1);

            var mLayoutManager = new LinearLayoutManager(this);
            mRecyclerView.SetLayoutManager(mLayoutManager);

            _downloadItemsAdapter = new DownloadItemsAdapter();
            mRecyclerView.SetAdapter(_downloadItemsAdapter);

            _downloadItemsAdapter.ItemClick += OnItemClick;

            _webView = FindViewById<WebView>(Resource.Id.webview);
            _webView.Settings.JavaScriptEnabled = true;
            _jsEngine = new WebViewJsEngine(_webView);

            _youtubeService = new YouTubeService(_jsEngine);
            _downloadService = new DownloadService(ApplicationContext, _youtubeService);
            _downloadService.OnDownloadStateChanged += OnDownloadStateChanged; ;
            _downloadService.OnDownloadError += OnDownloadError;

            HandleIntent(Intent);
#if DEBUG
            //HandleIntentTest();
#endif
        }

        protected override void OnNewIntent(Intent? intent)
        {
            _loadingVideoProgressRing.Visibility = Android.Views.ViewStates.Visible;
            _container.Visibility = Android.Views.ViewStates.Gone;
            HandleIntent(intent);
        }

        private void OnDownloadStateChanged(DownloadService.DownloadState state)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                switch (state.State)
                {
                    case Services.DownloadService.DownloadState.DownloadStage.FetchingData:
                        _downloadProgressBar.Indeterminate = true;
                        _downloadProgressBar.Visibility = Android.Views.ViewStates.Visible;
                        break;

                    case Services.DownloadService.DownloadState.DownloadStage.Downloading:
                        if (_downloadProgressBar.Indeterminate)
                        {
                            _downloadProgressBar.SetProgressCompat(0, true);
                        }
                        else
                        {
                            _downloadProgressBar.Progress = state.ProgressPercentage;
                        }
                        break;

                    case Services.DownloadService.DownloadState.DownloadStage.Muxing:
                        _downloadProgressBar.Indeterminate = true;
                        ShowSnakcbar("Muxing audio and video");
                        break;

                    case Services.DownloadService.DownloadState.DownloadStage.Completed:
                        ShowSnakcbar("Download finished");
                        _downloadProgressBar.Visibility = Android.Views.ViewStates.Invisible;
                        _isDownloading = false;
                        break;
                }
            });
        }

        private void OnDownloadError(Exception ex)
        {
            ShowSnakcbar($"Download error {ex.Message}");
            _isDownloading = false;
        }

        private async void OnItemClick(object? sender, int position)
        {
            if (_isDownloading) return;
            _isDownloading = true;

            var granted = await FileService.RequestPermissions();
            if (!granted)
            {
                _isDownloading = false;
                return;
            }

            var isPermissionRevoked = ContentResolver.PersistedUriPermissions.Count == 0;
            if (string.IsNullOrEmpty(AppSettings.DownloadsFolderPath) || isPermissionRevoked)
            {
                var folderPicker = new FolderPicker();
                var result = await folderPicker.PickFolderAsync();
                if (result == null)
                {
                    _isDownloading = false;
                    return;
                }
                AppSettings.DownloadsFolderPath = result.Uri;
            }
            ShowSnakcbar("Download started");
            var stream = _downloadItemsAdapter.Get(position);
            Task.Run(() => _downloadService.DownloadAsync(stream, VideoDataVM.VideoDetails));
        }

        private void ShowProgress(int percentage)
        {
            _downloadProgressBar.Progress = percentage;
        }

        private void OnTabSelected(object? sender, TabLayout.TabSelectedEventArgs e)
        {
            ChangeTab(e.Tab.Position);
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

        private void HandleIntent(Intent intent)
        {
            if (Intent.ActionSend == intent?.Action && intent.Type != null)
            {
                if (intent.Type.Contains("text/plain"))
                {
                    var sharedUrl = intent.GetStringExtra(Intent.ExtraText);
                    LogMessage($"HandleIntent {sharedUrl}");
                    Task.Run(() => GetVideoAsync(sharedUrl));
                }
            }
        }

        private async void HandleIntentTest()
        {
            var url = "https://www.youtube.com/watch?v=piEyKyJ4pFg";// "https://www.youtube.com/watch?v=0nUeIjPrsCM";
            Task.Run(() => GetVideoAsync(url));
        }

        private async Task GetVideoAsync(string url)
        {
            VideoDataVM = await _youtubeService.GetVideoData(url);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _tabLayout.RemoveAllTabs();
                foreach (var col in VideoDataVM.Collections)
                {
                    _tabLayout.AddTab(_tabLayout.NewTab().SetText(col.Container));
                }
                ChangeTab(0);

                var image = FindViewById<ImageView>(Resource.Id.thumbnailImage);
                Glide.With(this).Load(VideoDataVM.VideoDetails.ThumbnailUrl).Into(image);

                FindViewById<TextView>(Resource.Id.videoTitleText).Text = VideoDataVM.VideoDetails.Title;
                FindViewById<TextView>(Resource.Id.channelNameText).Text = VideoDataVM.VideoDetails.Channel;

                _loadingVideoProgressRing.Visibility = Android.Views.ViewStates.Gone;
                _container.Visibility = Android.Views.ViewStates.Visible;
            });
        }

        private void ChangeTab(int index)
        {
            _tabLayout.GetTabAt(index).Select();
            ShowStreams(index);
        }

        private void ShowStreams(int index)
        {
            var streams = new List<IStreamVM>();
            streams.Add(new HeaderVM { Label = "Audio" });
            streams.AddRange(VideoDataVM.Collections[index].AudioStreams);
            streams.Add(new HeaderVM { Label = "Video" });
            streams.AddRange(VideoDataVM.Collections[index].VideoStreams);

            _downloadItemsAdapter.Replace(streams);
        }

        private void ShowSnakcbar(string message)
        {
            Snackbar.Make(_container, message, Snackbar.LengthLong).Show();
        }

        public static void LogMessage(string text)
        {
            Log.Info("APP LOG", text);
            System.Diagnostics.Debug.WriteLine($"APP LOG: {text}");
        }
    }
}
