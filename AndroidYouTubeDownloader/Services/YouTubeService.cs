using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AndroidYouTubeDownloader.ViewModels;
using YouTubeStreamsExtractor;

namespace AndroidYouTubeDownloader.Services
{
    public class YouTubeService
    {
        private readonly YouTubeStreams _youTubeStreams;

        public YouTubeService()
        {
            var msgHandler = new Xamarin.Android.Net.AndroidMessageHandler()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };
            var httpClient = new HttpClient(msgHandler);
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            YouTubeStreams.ReplaceReqiredHeaders(httpClient);

            _youTubeStreams = new YouTubeStreams(httpClient);
        }

        public async Task<VideoDataVM> GetVideoData(string url)
        {
            var collections = new List<StreamCollectionVM>();

            var av01Codec = "av01";
            var avc1Codec = "avc1";
            var avoidCodec = av01Codec;

            url = GetFormattedUrl(url);
            var data = await _youTubeStreams.GetPlayerResponseAsync(url);

            var videoDetails = data.YTInitialPlayerResponse.VideoDetails;
            var videoDetailsVM = new VideoDetailsVM
            {
                Channel = videoDetails.Author ?? "",
                ThumbnailUrl = videoDetails.Thumbnail.Thumbnails.OrderByDescending(x => x.Width).FirstOrDefault()?.Url ?? "",
                Title = videoDetails.Title ?? ""
            };
            
            var streams = await _youTubeStreams.GetAllStreamsAsync(data);
            var containers = streams.Select(x => x.Container).Distinct().ToList();
            var streamSelector = new StreamSelector();

            foreach(var container in containers)
            {
                var collection = new StreamCollectionVM(container);

                var audios = streams.OfType<IAudioOnlyStreamInfo>()
                        .Where(x => x.Container == container);

                collection.AudioStreams.AddRange(
                    audios
                        .Select(x => new AudioStreamVM(x))
                        .OrderByDescending(x => x.AudioStream.Bitrate));

                collection.VideoStreams.AddRange(
                    streams.OfType<IVideoStreamInfo>()
                        .Where(x => x.Container == container)// && x.Codec != avoidCodec);
                        .OrderByDescending(x => x.Height)
                        .Select(x => new VideoStreamVM(x, streamSelector.SelectBestAudio(audios, container))));

                collections.Add(collection);
            }

            return new VideoDataVM
            {
                Collections = collections,
                VideoDetails = videoDetailsVM,
            };
        }

        public async Task<string> PrepareUrlAsync(IStreamInfo stream)
        {
            await stream.PlayableUrl.PrepareAsync(stream.RawUrl, _youTubeStreams.Decryptor);
            return stream.PlayableUrl.Url;
        }

        private string GetFormattedUrl(string url)
        {
            if (url.Contains("youtu.be"))
            {
                var id = url.Split('/').LastOrDefault();
                return $"https://www.youtube.com/watch?v={id}";
            }
            return url;
        }
    }
}
