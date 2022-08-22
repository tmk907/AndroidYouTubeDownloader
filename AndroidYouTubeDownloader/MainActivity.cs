using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using AndroidX.Annotations;
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
using SimpleFileDownloader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Essentials;
using YouTubeStreamsExtractor;

namespace AndroidYouTubeDownloader
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeTypes = new[] { "text/plain", })]
    public class MainActivity : Activity
    {
        private DownloadItemsAdapter _downloadItemsAdapter;
        private CircularProgressIndicator _progressBar;
        private ConstraintLayout _container;
        private TabLayout _tabLayout;
        private LinearProgressIndicator _downloadProgressBar;

        private VideoDataVM VideoDataVM;
        
        private YouTubeService _youtubeService;
        private DownloadService _downloadService;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            CrossCurrentActivity.Current.Init(this, savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            _progressBar = FindViewById<CircularProgressIndicator>(Resource.Id.progressBar);
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

            _youtubeService = new YouTubeService();
            _downloadService = new DownloadService(ApplicationContext,_youtubeService);
            _downloadService.OnDownloadProgressChanged += OnDownloadProgressChanged;
            _downloadService.OnDownloadError += OnDownloadError;
            _downloadService.OnDownloadFinished += OnDownloadFinished;

            HandleIntent();
#if DEBUG
            HandleIntentTest();
#endif
        }

        private void OnDownloadFinished()
        {
            ShowSnakcbar("Download finished");
        }

        private void OnDownloadError(Exception ex)
        {
            ShowSnakcbar($"Download error {ex.Message}");
        }

        private void OnDownloadProgressChanged(int progress)
        {
            ShowProgress(progress);
        }

        private async void OnItemClick(object? sender, int position)
        {
            var stream = _downloadItemsAdapter.Get(position);

            var granted = await FileService.RequestPermissions();
            if (!granted) return;

            if (string.IsNullOrEmpty(AppSettings.DownloadsFolderPath))
            {
                var folderPicker = new FolderPicker();
                var result = await folderPicker.PickFolderAsync();
                if (result == null) return;
                AppSettings.DownloadsFolderPath = result.Uri;
            }
            ShowSnakcbar("Download started");
            Task.Run(() => _downloadService.DownloadAsync(stream, VideoDataVM.VideoDetails));
        }

        private void ShowProgress(int percentage)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _downloadProgressBar.Progress = percentage;
            });
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

        private void HandleIntent()
        {
            if (Intent.ActionSend == Intent?.Action && Intent.Type != null)
            {
                if (Intent.Type.Contains("text/plain"))
                {
                    var sharedUrl = Intent.GetStringExtra(Intent.ExtraText);
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

                _progressBar.Visibility = Android.Views.ViewStates.Gone;
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
            Snackbar.Make(_container, message, Snackbar.LengthShort).Show();
        }

        public static void LogMessage(string text)
        {
            Log.Info("APP LOG", text);
            System.Diagnostics.Debug.WriteLine($"APP LOG: {text}");
        }
    }
}