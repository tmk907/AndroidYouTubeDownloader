using System.Collections.Generic;

namespace AndroidYouTubeDownloader.Services
{
    public class FFmpegMediaMuxerService : IMediaMuxer
    {
        public void Mux(string videoPath, string audioPath, string muxedPath, string container)
        {
            var arguments = new List<string>();
            arguments.Add("-i");
            arguments.Add(videoPath);
            arguments.Add("-i");
            arguments.Add(audioPath);
            arguments.Add("-codec");
            arguments.Add("copy");
            arguments.Add("-f");
            arguments.Add(container);
            arguments.Add("-y");
            arguments.Add(muxedPath);


            FFmpegKitSlim.FFmpegKitHelper.Execute(arguments.ToArray());
        }
    }
}
