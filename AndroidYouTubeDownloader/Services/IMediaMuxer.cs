namespace AndroidYouTubeDownloader.Services
{
    public interface IMediaMuxer
    {
        void Mux(string videoPath, string audioPath, string muxedPath, string container);
    }
}
