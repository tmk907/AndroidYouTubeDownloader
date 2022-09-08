using Android.Content;
using Android.OS;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.Fragment.App;
using AndroidX.Navigation;
using AndroidX.RecyclerView.Widget;
using AndroidYouTubeDownloader.Services;
using AndroidYouTubeDownloader.ViewModels;
using Bumptech.Glide;
using DryForest.Storage;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.Snackbar;
using Google.Android.Material.Tabs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace AndroidYouTubeDownloader
{
    internal class DownloadFragment : Fragment
    {
        public static INavDirections NavigateTo(string videoUrl) => new DownloadFragmentDirections(videoUrl);

        private View _view;

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

        private string _videoUrl;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            _view = inflater.Inflate(Resource.Layout.download_fragment, container, false);

            _loadingVideoProgressRing = _view.FindViewById<CircularProgressIndicator>(Resource.Id.loadingVideoProgressRing);
            _downloadProgressBar = _view.FindViewById<LinearProgressIndicator>(Resource.Id.dowloadProgressBar);
            _container = _view.FindViewById<ConstraintLayout>(Resource.Id.container);
            _tabLayout = _view.FindViewById<TabLayout>(Resource.Id.tabLayout1);
            _tabLayout.TabSelected += OnTabSelected;

            var mRecyclerView = _view.FindViewById<RecyclerView>(Resource.Id.recyclerView1);

            var mLayoutManager = new LinearLayoutManager(Context);
            mRecyclerView.SetLayoutManager(mLayoutManager);

            _downloadItemsAdapter = new DownloadItemsAdapter();
            mRecyclerView.SetAdapter(_downloadItemsAdapter);

            _downloadItemsAdapter.ItemClick += OnItemClick;

            _webView = _view.FindViewById<WebView>(Resource.Id.webview);
            _webView.Settings.JavaScriptEnabled = true;
            _jsEngine = new WebViewJsEngine(_webView);

            _youtubeService = new YouTubeService(_jsEngine);
            _downloadService = new DownloadService(Context, _youtubeService);
            _downloadService.OnDownloadStateChanged += OnDownloadStateChanged;
            _downloadService.OnDownloadError += OnDownloadError;

            return _view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            _videoUrl = Arguments.GetString("video_url");
            _isDownloading = false;
            if (!string.IsNullOrEmpty(_videoUrl))
            {
                _loadingVideoProgressRing.Visibility = Android.Views.ViewStates.Visible;
                _container.Visibility = Android.Views.ViewStates.Gone;
                Task.Run(() => GetVideoAsync(_videoUrl));
            }
        }

        //private void HandleIntent(Intent intent)
        //{
        //    _loadingVideoProgressRing.Visibility = Android.Views.ViewStates.Visible;
        //    _container.Visibility = Android.Views.ViewStates.Gone;

        //    if (Intent.ActionSend == intent?.Action && intent.Type != null)
        //    {
        //        if (intent.Type.Contains("text/plain"))
        //        {
        //            var sharedUrl = intent.GetStringExtra(Intent.ExtraText);
        //            Task.Run(() => GetVideoAsync(sharedUrl));
        //        }
        //    }
        //}

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
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ShowSnakcbar($"Download error {ex.Message}");
                _downloadProgressBar.Visibility = Android.Views.ViewStates.Invisible;
            });
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

            var isPermissionRevoked = Context.ContentResolver.PersistedUriPermissions.Count == 0;
            if (string.IsNullOrEmpty(AppSettings.DownloadsFolderPath) || isPermissionRevoked)
            {
                var folderPicker = new FolderPicker();
                var result = await folderPicker.PickFolderAsync();
                if (result == null)
                {
                    _isDownloading = false;
                    return;
                }
                AppSettings.ChangeDownloadsFolder(result.Uri, result.Name);
            }
            ShowSnakcbar("Download started");
            var stream = _downloadItemsAdapter.Get(position);
            Task.Run(() => _downloadService.DownloadAsync(stream, VideoDataVM.VideoDetails));
        }

        private void OnTabSelected(object? sender, TabLayout.TabSelectedEventArgs e)
        {
            ChangeTab(e.Tab.Position);
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

                var image = _view.FindViewById<ImageView>(Resource.Id.thumbnailImage);
                Glide.With(this).Load(VideoDataVM.VideoDetails.ThumbnailUrl).Into(image);

                _view.FindViewById<TextView>(Resource.Id.videoTitleText).Text = VideoDataVM.VideoDetails.Title;
                _view.FindViewById<TextView>(Resource.Id.channelNameText).Text = VideoDataVM.VideoDetails.Channel;

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
    }

    public class DownloadFragmentDirections : Java.Lang.Object, INavDirections
    {
        private readonly Bundle _bundle;

        public DownloadFragmentDirections(string videoUrl)
        {
            _bundle = new Bundle();
            _bundle.PutString("video_url", videoUrl);
        }

        public int ActionId => Resource.Id.action_global_downloadFragment;

        public Bundle Arguments => _bundle;
    }
}
