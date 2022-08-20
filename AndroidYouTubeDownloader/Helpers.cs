using System.Globalization;

namespace AndroidYouTubeDownloader
{
    public class Helpers
    {
        public static string ToMBLabel(long bytes)
        {
            double mb = bytes / (1024.0 * 1024);
            return string.Format(CultureInfo.InvariantCulture, "{0:N2} MB", mb);
        }

        public static string ToBitrateLabel(long bitrate)
        {
            return $"{bitrate / 1024} kb/s";
        }
    }
}
