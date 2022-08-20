using YouTubeStreamsExtractor;

namespace AndroidYouTubeDownloader.ViewModels
{
    public class AudioStreamVM : IStreamVM
    {
        public AudioStreamVM(IAudioStreamInfo audioStream)
        {
            AudioStream = audioStream;

            BitrateLabel = Helpers.ToBitrateLabel(audioStream.Bitrate);
            Size = Helpers.ToMBLabel(audioStream.ContentLength);
        }

        public string BitrateLabel { get; }
        public string Size { get; }

        public string Label
        {
            get
            {
                return $"{Size} {BitrateLabel} ({AudioStream.Codec})";
            }
        }

        public IAudioStreamInfo AudioStream { get; init; }
    }
}
