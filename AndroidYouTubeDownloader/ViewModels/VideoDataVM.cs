using System.Collections.Generic;

namespace AndroidYouTubeDownloader.ViewModels
{
    internal class VideoDataVM
    {
        public VideoDetailsVM VideoDetails { get; init; }
        public List<StreamCollectionVM> Collections { get; init; }
    }
}
