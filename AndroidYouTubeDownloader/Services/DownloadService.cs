using System;
using System.IO;
using System.Threading.Tasks;
using AndroidYouTubeDownloader.ViewModels;
using DryForest.Storage;
using YouTubeStreamsExtractor;
using Android.Content;
using Xamarin.Essentials;
using System.Threading;
using System.Net.Http;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Square.OkHttp;
using ATL;

namespace AndroidYouTubeDownloader.Services
{
    public class DownloadService
    {
        private readonly Context _context;
        private readonly YouTubeService _youtubeService;
        private readonly HttpClient _httpClient;
        private readonly OkHttpClient _okHttpClient;

        public DownloadService(Context context, YouTubeService youTubeService)
        {
            _context = context;
            _youtubeService = youTubeService;
            _httpClient = new HttpClient();
            _okHttpClient = new OkHttpClient();
        }

        public event Action<DownloadState> OnDownloadStateChanged;

        public event Action<Exception> OnDownloadError;

        public class DownloadState
        {
            public DownloadState(DownloadStage state, int progressPercentage)
            {
                State = state;
                ProgressPercentage = progressPercentage;
            }

            public DownloadStage State { get; }

            public int ProgressPercentage { get; }

            public enum DownloadStage
            {
                FetchingData,
                Downloading,
                Muxing,
                Completed
            }
        }

        public async Task DownloadAsync(IStreamVM stream, VideoDetailsVM videoDetails)
        {
            try
            {
                OnDownloadStateChanged?.Invoke(new DownloadState(DownloadState.DownloadStage.FetchingData, 0));

                if (stream is AudioStreamVM audio)
                {
                    var tempFile = await DownloadToTemporaryFile(audio.AudioStream).ConfigureAwait(false);
                    var audioFile = Path.ChangeExtension(tempFile, audio.AudioStream.Container);
                    File.Move(tempFile, audioFile);

                    try
                    {
                        var thumbnailData = await _httpClient.GetByteArrayAsync(videoDetails.ThumbnailUrl).ConfigureAwait(false);
                        
                        var track = new Track(audioFile);
                        PictureInfo newPicture = PictureInfo.fromBinaryData(thumbnailData);
                        track.EmbeddedPictures.Add(newPicture);
                        track.Title = videoDetails.Title;
                        track.Artist = videoDetails.Channel;
                        track.Comment = videoDetails.Url;
                        track.Save();
                    }
                    catch (Exception ex)
                    {

                    }

                    await CopyToTargetFile(audioFile, audio.AudioStream, videoDetails).ConfigureAwait(false);
                    OnDownloadStateChanged?.Invoke(new DownloadState(DownloadState.DownloadStage.Completed, 0));
                }
                else if (stream is VideoStreamVM video)
                {
                    if (video.AudioStream == null)
                    {
                        var file = await DownloadToTemporaryFile(video.VideoStream).ConfigureAwait(false);
                        await CopyToTargetFile(file, video.VideoStream, videoDetails).ConfigureAwait(false);
                    }
                    else
                    {
                        var totalLength = video.AudioStream.ContentLength + video.VideoStream.ContentLength;

                        var videoFile = await DownloadToTemporaryFile(video.VideoStream).ConfigureAwait(false);
                        var audioFile = await DownloadToTemporaryFile(video.AudioStream).ConfigureAwait(false);

                        OnDownloadStateChanged?.Invoke(new DownloadState(DownloadState.DownloadStage.Muxing, 0));

                        var muxedPath = Java.IO.File.CreateTempFile("temp", null, _context.CacheDir).AbsolutePath;
                        //var mediaMuxer = new MediaMuxerService();
                        var mediaMuxer = new FFmpegMediaMuxerService();
                        mediaMuxer.Mux(videoFile, audioFile, muxedPath, video.VideoStream.Container, videoDetails);

                        await CopyToTargetFile(muxedPath, video.VideoStream, videoDetails).ConfigureAwait(false);

                        OnDownloadStateChanged?.Invoke(new DownloadState(DownloadState.DownloadStage.Completed, 0));
                    }
                }
            }
            catch (Exception ex)
            {
                OnDownloadError?.Invoke(ex);
            }

            ClearCache();
        }

        private async Task<string> DownloadToTemporaryFile(IStreamInfo stream)
        {
            var tempFilePath = Java.IO.File.CreateTempFile("temp", null, _context.CacheDir).AbsolutePath;

            await _youtubeService.PrepareUrlAsync(stream).ConfigureAwait(false);

            var contentLength = await GetContentLength(stream.PlayableUrl.Url);
            if (contentLength == 0) contentLength = stream.ContentLength;
            var progress = new Progress<double>((e) =>
            {
                var p = (int)(e / contentLength * 100);
                OnDownloadStateChanged?.Invoke(new DownloadState(DownloadState.DownloadStage.Downloading, p));
            });
            using var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Write);
            await DownloadRanges(stream.PlayableUrl.Url, contentLength, fileStream, progress).ConfigureAwait(false);
            
            return tempFilePath;
        }

        private async Task CopyToTargetFile(string tempFilePath, IStreamInfo stream, VideoDetailsVM video)
        {
            var fileName = FileService.RemoveForbiddenChars(video.Title);
            var extension = stream.Container;
            var mimeType = stream.MimeType;
            if (stream.Codec.Contains("opus"))
            {
                mimeType = "audio/ogg";
                extension = "oga";
            }
            if (mimeType == "audio/mp4")
            {
                extension = "m4a";
            }
            var downloadsFolder = new StorageItem(AppSettings.DownloadsFolderPath);
            var files = await downloadsFolder.GetFilesAsync();
            var fileNameSuffix = "";
            var max = 22;
            for(int i = 2; i <= max; i++)
            {
                if (files.Any(x=>x.Name == $"{fileName} {fileNameSuffix}.{extension}"))
                {
                    fileNameSuffix = $"({i})";
                }
                else
                {
                    break;
                }
            }

            if (fileNameSuffix == $"({max})") throw new Exception("File with same name exists");
            
            var audioFile = await downloadsFolder
                .CreateFileAsync($"{fileName}{fileNameSuffix}", mimeType).ConfigureAwait(false);
            using var inputFile = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read);
            using var outputFile = await audioFile.OpenStreamAsync(FileAccess.Write);
            await inputFile.CopyToAsync(outputFile).ConfigureAwait(false);
        }

        private void ClearCache()
        {
            var dir = new DirectoryInfo(FileSystem.CacheDirectory);
            foreach (FileInfo file in dir.GetFiles())
            {
                file.Delete();
            }
        }

        private async Task DownloadRanges(string url, long contentLength, Stream destination,
            IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            var ranges = CreateRanges(contentLength, 8);
            foreach(var range in ranges)
            {
                //await DownloadRange(url, range, destination, progress, cancellationToken).ConfigureAwait(false);
                //await DownloadRangeUsingWebRequest(url, range, destination, progress, cancellationToken).ConfigureAwait(false);
                await DownloadRangeUsingOkHttp(url, range, destination, progress, cancellationToken).ConfigureAwait(false);
            }
        }


        private async Task DownloadRange(string url, (long,long) range, Stream destination, 
            IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Version = HttpVersion.Version11;
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Host", new Uri(url).Host);
            request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.114 Safari/537.36");
            request.Headers.Add("Range", $"bytes={range.Item1}-{range.Item2}");

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            using (var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false))
            {
                await contentStream.CopyToAsync(destination, progress, range.Item1, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task DownloadRangeUsingWebRequest(string url, (long, long) range, Stream destination,
            IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Method = "GET";
            request.ProtocolVersion = HttpVersion.Version11;
            request.AddRange(range.Item1, range.Item2);
            request.Accept = "*/*";
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.134 Safari/537.36 OPR/89.0.4447.101";

            
            using HttpWebResponse downloadResponse = request.GetResponse() as HttpWebResponse;
            if (downloadResponse.StatusCode == HttpStatusCode.OK ||
                downloadResponse.StatusCode == HttpStatusCode.PartialContent ||
                downloadResponse.StatusCode == HttpStatusCode.Created ||
                downloadResponse.StatusCode == HttpStatusCode.Accepted ||
                downloadResponse.StatusCode == HttpStatusCode.ResetContent)
            {
                using Stream responseStream = downloadResponse?.GetResponseStream();
                if (responseStream != null)
                {
                    await responseStream.CopyToAsync(destination, progress, range.Item1, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                throw new Exception("HttpWebResponse error");
            }
        }

        private async Task DownloadRangeUsingOkHttp(string url, (long, long) range, Stream destination,
            IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            var request = new Request.Builder()
                .Url(url)
                .Header("Accept", "*/*")
                .Header("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.5060.134 Safari/537.36 OPR/89.0.4447.101")
                .Header("Range", $"bytes={range.Item1}-{range.Item2}")
                .Build();

            using var response = await _okHttpClient.NewCall(request).ExecuteAsync();
            if (response.IsSuccessful)
            {
                using Stream responseStream = response.Body().ByteStream();
                if (responseStream != null)
                {
                    await responseStream.CopyToAsync(destination, progress, range.Item1, cancellationToken).ConfigureAwait(false);
                }
            }
        }


        private List<(long, long)> CreateRanges(long fileSize, long parts)
        {
            var start = 0;
            var ranges = new List<(long, long)>();
            long rangeSize = fileSize / parts;
            for (int i = 0; i < parts; i++)
            {
                long startPosition = start + (i * rangeSize);
                long endPosition = startPosition + rangeSize - 1;
                ranges.Add((startPosition, endPosition));
            }
            var lastRange = ranges.Last();
            lastRange.Item2 += fileSize % parts;
            ranges.RemoveAt((int)parts - 1);
            ranges.Add(lastRange);

            return ranges;
        }

        private async Task<long> GetContentLength(string url)
        {
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            request.Headers.Add("Accept", "*/*");
            request.Headers.Add("Host", new Uri(url).Host);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return response.Content.Headers.ContentLength ?? 0;
            }
            return 0;
        }
    }
}
