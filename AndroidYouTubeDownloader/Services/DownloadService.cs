using System;
using System.IO;
using System.Threading.Tasks;
using AndroidYouTubeDownloader.ViewModels;
using DryForest.Storage;
using SimpleFileDownloader;
using YouTubeStreamsExtractor;
using Android.Content;
using Downloader;

namespace AndroidYouTubeDownloader.Services
{
    public class DownloadService
    {
        private readonly Context _context;
        private readonly YouTubeService _youtubeService;
        private readonly DownloadConfiguration _downloadConfiguration;

        public DownloadService(Context context, YouTubeService youTubeService)
        {
            _context = context;
            _youtubeService = youTubeService;
            _downloadConfiguration = FileDownloader.DefaulConfiguration;
            _downloadConfiguration.ParallelDownload = true;
            _downloadConfiguration.ChunkCount = 8;
        }

        public event Action OnDownloadFinished;

        public event Action<int> OnDownloadProgressChanged;

        public event Action<Exception> OnDownloadError;


        public async Task DownloadAsync(IStreamVM stream, VideoDetailsVM videoDetails)
        {
            try
            {
                if (stream is AudioStreamVM audio)
                {
                    var file = await DownloadToTemporaryFile(audio.AudioStream);
                    await CopyToTargetFile(file, audio.AudioStream, videoDetails);

                    //var thumbnailFile = await downloadsFolder.CreateFileAsync(fileName, MimeTypes.MimeTypeMap.GetMimeType(".jpg"));
                    //using (var fileStream = await thumbnailFile.OpenStreamAsync(FileAccess.ReadWrite))
                    //{
                    //    await downloadService.Download(VideoDataVM.VideoDetails.ThumbnailUrl, fileStream);
                    //}

                    OnDownloadFinished?.Invoke();
                }
                else if (stream is VideoStreamVM video)
                {
                    if (video.AudioStream == null)
                    {
                        var file = await DownloadToTemporaryFile(video.VideoStream);
                        await CopyToTargetFile(file, video.VideoStream, videoDetails);
                    }
                    else
                    {
                        var totalLength = video.AudioStream.ContentLength + video.VideoStream.ContentLength;

                        var videoFile = await DownloadToTemporaryFile(video.VideoStream);
                        var audioFile = await DownloadToTemporaryFile(video.AudioStream);

                        var muxedPath = Java.IO.File.CreateTempFile("temp", null, _context.CacheDir).AbsolutePath;
                        var mediaMuxer = new MediaMuxerService(_context);
                        mediaMuxer.Mux(videoFile, audioFile, muxedPath, video.VideoStream.Container);

                        await CopyToTargetFile(muxedPath, video.VideoStream, videoDetails);

                        OnDownloadFinished?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                OnDownloadError?.Invoke(ex);
            }
        }

        private async Task<string> DownloadToTemporaryFile(IStreamInfo stream)
        {
            var tempFilePath = Java.IO.File.CreateTempFile("temp", null, _context.CacheDir).AbsolutePath;

            await _youtubeService.PrepareUrlAsync(stream);

            var downloader = new FileDownloader(tempFilePath);
            downloader.OnDownloadProgressChanged += (s, e) =>
            {
                OnDownloadProgressChanged?.Invoke((int)e.ProgressPercentage);
            };
            await downloader.DownloadAsync(stream.PlayableUrl.Url);

            return tempFilePath;
        }

        private async Task CopyToTargetFile(string tempFilePath, IStreamInfo stream, VideoDetailsVM video)
        {
            var fileName = FileService.RemoveForbiddenChars(video.Title);
            var extension = stream.Container;
            //if (stream is IAudioOnlyStreamInfo && stream.Container == "mp4")
            //{
            //    extension = "m4a";
            //};
            var downloadsFolder = new StorageItem(AppSettings.DownloadsFolderPath);
            var audioFile = await downloadsFolder.CreateFileAsync($"{fileName}.{extension}", stream.MimeType);

            using var inputFile = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read);
            using var outputFile = await audioFile.OpenStreamAsync(FileAccess.Write);
            inputFile.CopyTo(outputFile);
        }

    }
}
