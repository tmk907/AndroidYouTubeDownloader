using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;

namespace AndroidYouTubeDownloader.Services
{
    public class DownloadService
    {
        private readonly HttpClient _httpClient;

        public DownloadService()
        {
            _httpClient = new HttpClient();
        }

        public async Task Download(string url, Stream destination, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var httpStream = await _httpClient.GetStreamAsync(url))
                {
                    await httpStream.CopyToAsync(destination, cancellationToken);
                }
            }
            catch (Exception ex)
            {
            }
        }

        public async Task Download(string url, Stream destination,
            IProgress<double> progress, CancellationToken cancellationToken = default)
        {
            try
            {
                using (var httpStream = await _httpClient.GetStreamAsync(url))
                {
                    await httpStream.CopyToAsync(destination, progress, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                //TODO
                progress.Report(1);
            }
        }
    }
}
