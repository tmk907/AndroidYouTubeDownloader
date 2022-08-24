using Android.Webkit;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;
using YouTubeStreamsExtractor;

namespace AndroidYouTubeDownloader
{
    public partial class MainActivity
    {
        public class WebViewJsEngine : IJavaScriptEngine
        {
            private readonly WebView _webView;
            private ValueCallback _webviewCallback;

            public WebViewJsEngine(WebView webView)
            {
                _webView = webView;
                _webviewCallback = new ValueCallback();
                _webviewCallback.OnResultReceived += OnResultReceived;
            }

            public async Task<string> ExecuteJSCodeAsync(string code, string functionName, string argument)
            {
                var script = $"{code}; {functionName}('{argument}')";

                var tcs = new TaskCompletionSource<string>();
                Action<string> callback = null;
                callback = (result) =>
                {
                    _webviewCallback.OnResultReceived -= callback;
                    tcs.SetResult(result);
                };
                _webviewCallback.OnResultReceived += callback;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _webView.EvaluateJavascript(script, _webviewCallback);
                });

                var result = await tcs.Task;

                return result;
            }

            private void OnResultReceived(string obj)
            {

            }

            class ValueCallback : Java.Lang.Object, IValueCallback
            {
                public event Action<string> OnResultReceived;

                public void OnReceiveValue(Java.Lang.Object? value)
                {
                    var res = value.ToString().Trim('"');
                    OnResultReceived?.Invoke(res);
                }
            }
        }
    }
}