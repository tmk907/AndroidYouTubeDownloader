using YouTubeStreamsExtractor;

namespace AndroidYouTubeDownloader.ViewModels
{
    public class VideoStreamVM : IStreamVM
    {
        public VideoStreamVM(IVideoStreamInfo videoStream, IAudioStreamInfo? audioStream)
        {
            VideoStream = videoStream;
            AudioStream = audioStream;
            if (videoStream is IMuxedStreamInfo)
            {
                AudioStream = null;
            }

            if (videoStream.ContentLength == 0)
            {
                TotalSize = "? MB";
            }
            else
            {
                if (audioStream == null || audioStream.ContentLength != 0)
                {
                    TotalSize = Helpers.ToMBLabel(videoStream.ContentLength + audioStream?.ContentLength ?? 0);
                }
                else
                {
                    TotalSize = $"> {Helpers.ToMBLabel(videoStream.ContentLength)}";
                }
            }
        }

        public string TotalSize { get; }

        public string Label
        {
            get
            {
                return $"{VideoStream.QualityLabel} {TotalSize} ({VideoStream.Codec})";
            }
        }

        public IVideoStreamInfo VideoStream { get; init; }
        public IAudioStreamInfo? AudioStream { get; init; }
    }
}
