using System.Collections.Generic;

namespace AndroidYouTubeDownloader.ViewModels
{
    public class StreamCollectionVM
    {
        public StreamCollectionVM(string container)
        {
            Container = container;
        }

        public string Container { get; }

        public List<AudioStreamVM> AudioStreams { get; } = new List<AudioStreamVM>();

        public List<VideoStreamVM> VideoStreams { get; } = new List<VideoStreamVM>();
    }
}
