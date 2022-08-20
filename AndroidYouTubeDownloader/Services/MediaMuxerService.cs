using Android.Content;
using Android.Media;
using Java.Nio;
using System.IO;
using Xamarin.Essentials;

namespace AndroidYouTubeDownloader.Services
{
    public class MediaMuxerService
    {
        private readonly Context _context;

        public MediaMuxerService(Context context)
        {
            _context = context;
        }

        public string Mux(string videoUri, string audioUri, string container)
        {

            MuxerOutputType format;
            if (container == "mp4")
            {
                format = MuxerOutputType.Mpeg4;
            }
            else if (container == "webm")
            {
               format= MuxerOutputType.Webm;
            }
            else
            {
                return "";
            }

            var cacheDir = FileSystem.CacheDirectory;
            var outputPath = Path.Combine(cacheDir, "output.video");

            // https://sisik.eu/blog/android/media/mix-audio-into-video
            MediaMuxer muxer = new MediaMuxer(outputPath, format);

            var videoExtractor = new MediaExtractor();
            videoExtractor.SetDataSource(_context, Android.Net.Uri.Parse(videoUri), null);
            videoExtractor.SelectTrack(0); // Assuming only one track per file. Adjust code if this is not the case.
            var videoFormat = videoExtractor.GetTrackFormat(0);

            var audioExtractor = new MediaExtractor();
            audioExtractor.SetDataSource(_context, Android.Net.Uri.Parse(audioUri), null);
            audioExtractor.SelectTrack(0); // Assuming only one track per file. Adjust code if this is not the case.
            var audioFormat = audioExtractor.GetTrackFormat(0);

            // Init muxer
            var videoIndex = muxer.AddTrack(videoFormat);
            var audioIndex = muxer.AddTrack(audioFormat);
            muxer.Start();

            // Prepare buffer for copying
            var maxChunkSize = 1024 * 1024;
            var buffer = ByteBuffer.Allocate(maxChunkSize);
            var bufferInfo = new MediaCodec.BufferInfo();

            // Copy Video
            while (true)
            {
                var chunkSize = videoExtractor.ReadSampleData(buffer, 0);

                if (chunkSize > 0)
                {
                    bufferInfo.PresentationTimeUs = videoExtractor.SampleTime;
                    bufferInfo.Flags = (MediaCodecBufferFlags)videoExtractor.SampleFlags;
                    bufferInfo.Size = chunkSize;

                    muxer.WriteSampleData(videoIndex, buffer, bufferInfo);

                    videoExtractor.Advance();

                }
                else
                {
                    break;
                }
            }

            while (true)
            {
                var chunkSize = audioExtractor.ReadSampleData(buffer, 0);

                if (chunkSize >= 0)
                {
                    bufferInfo.PresentationTimeUs = audioExtractor.SampleTime;
                    bufferInfo.Flags = (MediaCodecBufferFlags)audioExtractor.SampleFlags;
                    bufferInfo.Size = chunkSize;

                    muxer.WriteSampleData(audioIndex, buffer, bufferInfo);
                    audioExtractor.Advance();
                }
                else
                {
                    break;
                }
            }

            muxer.Stop();
            muxer.Release();

            videoExtractor.Release();
            audioExtractor.Release();

            return outputPath;
        }
    }
}
