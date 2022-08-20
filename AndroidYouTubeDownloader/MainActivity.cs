using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Widget;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.RecyclerView.Widget;
using AndroidYouTubeDownloader.Services;
using AndroidYouTubeDownloader.ViewModels;
using Bumptech.Glide;
using DryForest.Storage;
using Google.Android.Material.ProgressIndicator;
using Google.Android.Material.Tabs;
using Plugin.CurrentActivity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace AndroidYouTubeDownloader
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    [IntentFilter(new[] { Intent.ActionSend }, Categories = new[] { Intent.CategoryDefault }, DataMimeTypes = new[] { "text/plain", })]
    public class MainActivity : Activity
    {
        private static string sharedUrl = "";

        private DownloadItemsAdapter _downloadItemsAdapter;
        private CircularProgressIndicator _progressBar;
        private ConstraintLayout _container;
        private TabLayout _tabLayout;
        private LinearProgressIndicator _downloadProgressBar;

        private YouTubeService _youtubeService;

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

            HandleIntent();
            //HandleIntentTest();
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
            Task.Run(() => DownloadAsync(stream));
        }

        private async Task DownloadAsync(IStreamVM stream)
        {
            try
            {
                var downloadService = new DownloadService();
                var downloadsFolder = new StorageItem(AppSettings.DownloadsFolderPath);

                if (stream is AudioStreamVM audio)
                {
                    await _youtubeService.PrepareUrlAsync(audio.AudioStream);

                    var fileName = FileService.RemoveForbiddenChars(VideoDataVM.VideoDetails.Title);
                    var extension = audio.AudioStream.Container == "mp4" ? "m4a" : audio.AudioStream.Codec;

                    var thumbnailFile = await downloadsFolder.CreateFileAsync(fileName, MimeTypes.MimeTypeMap.GetMimeType(".jpg"));
                    using (var fileStream = await thumbnailFile.OpenStreamAsync(FileAccess.ReadWrite))
                    {
                        await downloadService.Download(VideoDataVM.VideoDetails.ThumbnailUrl, fileStream);
                    }

                    var progress = new Progress<double>((e) =>
                    {
                        var p = (int)(e / audio.AudioStream.ContentLength * 100);
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            _downloadProgressBar.Progress = p;
                        });
                        LogMessage($"progress {p} {e / audio.AudioStream.ContentLength} %");
                    });
                    var audioFile = await downloadsFolder.CreateFileAsync(fileName, MimeTypes.MimeTypeMap.GetMimeType($".{extension}"));
                    using (var fileStream = await audioFile.OpenStreamAsync(FileAccess.ReadWrite))
                    {
                        await downloadService.Download(audio.AudioStream.PlayableUrl.Url, fileStream, progress);
                    }

                }
                else if (stream is VideoStreamVM video)
                {
                    var fileName = FileService.RemoveForbiddenChars(VideoDataVM.VideoDetails.Title);

                    await _youtubeService.PrepareUrlAsync(video.VideoStream);
                    if (video.AudioStream == null)
                    {
                        var videoFile = await downloadsFolder.CreateFileAsync(fileName, video.VideoStream.MimeType);

                        using (var fileStream = await videoFile.OpenStreamAsync(FileAccess.ReadWrite))
                        {
                            await downloadService.Download(video.VideoStream.PlayableUrl.Url, fileStream);
                        }
                    }
                    else
                    {
                        var totalLength = video.AudioStream.ContentLength + video.VideoStream.ContentLength;
                        var progress = new Progress<double>((e) =>
                        {
                            var p = (int)(e / totalLength * 100);
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                _downloadProgressBar.Progress = p;
                            });
                            LogMessage($"progress {p} {e / totalLength} %");
                        });
                        var tempVideoFile = await downloadsFolder.CreateFileAsync($"{fileName}.vtemp", video.VideoStream.MimeType);
                        using (var fileStream = await tempVideoFile.OpenStreamAsync(FileAccess.ReadWrite))
                        {
                            await downloadService.Download(video.VideoStream.PlayableUrl.Url, fileStream, progress);
                        }

                        progress = new Progress<double>((e) =>
                        {
                            var p = (int)((e + video.VideoStream.ContentLength) / totalLength * 100);
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                _downloadProgressBar.Progress = p;
                            });
                            LogMessage($"progress {p} {(e + video.VideoStream.ContentLength) / totalLength} %");
                        });
                        await _youtubeService.PrepareUrlAsync(video.AudioStream);
                        var tempAudioFile = await downloadsFolder.CreateFileAsync($"{fileName}.atemp", video.AudioStream.MimeType);
                        using (var fileStream = await tempAudioFile.OpenStreamAsync(FileAccess.ReadWrite))
                        {
                            await downloadService.Download(video.AudioStream.PlayableUrl.Url, fileStream, progress);
                        }

                        var videoFile = await downloadsFolder.CreateFileAsync($"{fileName}", video.VideoStream.MimeType);
                        var mediaMuxer = new MediaMuxerService(this);
                        var outputPath = mediaMuxer.Mux(tempVideoFile.Uri, tempAudioFile.Uri, video.VideoStream.Container);
                        using (var inputFileStream = new FileStream(outputPath, FileMode.Open))
                        using (var outputFileStream = await videoFile.OpenStreamAsync(FileAccess.ReadWrite))
                        {
                            await inputFileStream.CopyToAsync(outputFileStream);
                        }
                        await tempAudioFile.Delete();
                        await tempVideoFile.Delete();
                    }
                }
            }
            catch (Exception ex)
            {

            }
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _downloadProgressBar.Progress = 0;
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
                    sharedUrl = Intent.GetStringExtra(Intent.ExtraText);
                    LogMessage($"HandleIntent {sharedUrl}");
                    Task.Run(() => GetVideoAsync(sharedUrl));
                }
            }
        }

        private async void HandleIntentTest()
        {
            sharedUrl = "https://www.youtube.com/watch?v=piEyKyJ4pFg";// "https://www.youtube.com/watch?v=0nUeIjPrsCM";
            Task.Run(() => GetVideoAsync(sharedUrl));
        }

        private VideoDataVM VideoDataVM;

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

        public static void LogMessage(string text)
        {
            Log.Info("APP LOG", text);
            System.Diagnostics.Debug.WriteLine($"APP LOG: {text}");
        }
    }
}