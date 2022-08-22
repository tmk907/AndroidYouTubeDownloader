using System.Collections.Generic;

namespace AndroidYouTubeDownloader.ViewModels
{
    public class VideoDataVM
    {
        public VideoDetailsVM VideoDetails { get; init; }
        public List<StreamCollectionVM> Collections { get; init; }
    }
}
