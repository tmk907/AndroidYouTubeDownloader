using AndroidYouTubeDownloader.ViewModels;
using CliWrap.Builders;

namespace AndroidYouTubeDownloader.Services
{
    public class FFmpegMediaMuxerService : IMediaMuxer
    {
        public void Mux(string videoPath, string audioPath, string muxedPath, string container, VideoDetailsVM videoDetails)
        {
            var arguments = new ArgumentsBuilder();
            arguments.Add("-i");
            arguments.Add(videoPath);
            arguments.Add("-i");
            arguments.Add(audioPath);
            arguments.Add("-codec");
            arguments.Add("copy");

            arguments.Add("-metadata").Add($"title=\"{videoDetails.Title.Replace("\"", "\\\"")}\"", false);
            arguments.Add("-metadata").Add($"artist=\"{videoDetails.Channel}\"", false);
            arguments.Add("-metadata").Add($"comment=\"{videoDetails.Url}\"", false);

            arguments.Add("-f");
            arguments.Add(container);
            arguments.Add("-y");
            arguments.Add(muxedPath);

            var args = arguments.Build();
            FFmpegKitSlim.FFmpegKitHelper.Execute(args);
        }
    }
}
