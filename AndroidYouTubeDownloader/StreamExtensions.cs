using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AndroidYouTubeDownloader
{
    internal static class StreamExtensions
    {
        public static async Task CopyToAsync(this Stream source, Stream destination,
            IProgress<double>? progress = null, long progressOffset = 0, CancellationToken cancellationToken = default)
        {
            var buffer = new byte[1024 * 8 * 10];
            var totalBytesCopied = 0L;
            int bytesCopied;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();

                bytesCopied = await source.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                await destination.WriteAsync(buffer, 0, bytesCopied).ConfigureAwait(false);

                totalBytesCopied += bytesCopied;
                progress?.Report(totalBytesCopied + progressOffset);
            } while (bytesCopied > 0);
            progress?.Report(totalBytesCopied + progressOffset);
        }
    }
}