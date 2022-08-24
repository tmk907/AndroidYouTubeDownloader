using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Plugin.CurrentActivity;

namespace DryForest.Storage
{
    public partial class FolderPicker : IFolderPicker
    {
        public const int BROWSE_REQUEST_CODE = 100;

        private int requestId;
        private TaskCompletionSource<IStorageItem> folderCompletionSource;

        public Task<IStorageItem> PickFolderAsync()
        {
            var id = this.GetRequestId();

            var next = new TaskCompletionSource<IStorageItem>(id);

            // Interlocked.CompareExchange(ref object location1, object value, object comparand)
            // Compare location1 with comparand.
            // If equal replace location1 by value.
            // Returns the original value of location1.
            // ---
            // In this context, tcs is compared to null, if equal tcs is replaced by next,
            // and original tcs is returned.
            // We then compare original tcs with null, if not null it means that a task was 
            // already started.
            if (Interlocked.CompareExchange(ref folderCompletionSource, next, null) != null)
            {
                folderCompletionSource.TrySetResult(null);
            }
            try
            {
                BrowserFolder(CrossCurrentActivity.Current.Activity, BROWSE_REQUEST_CODE);

                Action<Android.Net.Uri> folderPicked = null;
                Action folderPickCanceled = null;

                folderPicked = (uri) =>
                {
                    var tcs = Interlocked.Exchange(ref this.folderCompletionSource, null);

                    FolderPickerHelper.FolderPicked -= folderPicked;
                    FolderPickerHelper.FolderPickCanceled -= folderPickCanceled;

                    var storage = new StorageItem(uri);
                    tcs?.SetResult(storage);
                };

                folderPickCanceled = () =>
                {
                    var tcs = Interlocked.Exchange(ref this.folderCompletionSource, null);

                    FolderPickerHelper.FolderPicked -= folderPicked;
                    FolderPickerHelper.FolderPickCanceled -= folderPickCanceled;

                    tcs?.SetResult(null);
                };

                FolderPickerHelper.FolderPicked += folderPicked;
                FolderPickerHelper.FolderPickCanceled += folderPickCanceled;

            }
            catch (Exception ex)
            {
                folderCompletionSource.SetException(ex);
            }

            return folderCompletionSource.Task;
        }

        public static void BrowserFolder(Activity activity, int requestCode)
        {
            using (var intent = new Intent(Intent.ActionOpenDocumentTree))
            {
                //intent.AddFlags(ActivityFlags.GrantReadUriPermission);
                //intent.AddFlags(ActivityFlags.GrantWriteUriPermission);
                //intent.AddFlags(ActivityFlags.GrantPersistableUriPermission);
                //intent.AddFlags(ActivityFlags.GrantPrefixUriPermission);
                intent.PutExtra("android.content.extra.SHOW_ADVANCED", true);
                intent.PutExtra("android.content.extra.FANCY", true);
                activity.StartActivityForResult(intent, requestCode);
            }
        }

        private int GetRequestId()
        {
            int id = this.requestId;

            if (this.requestId == int.MaxValue)
            {
                this.requestId = 0;
            }
            else
            {
                this.requestId++;
            }

            return id;
        }
    }
}
