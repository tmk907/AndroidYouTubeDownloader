using System;
using Android.App;
using Android.Content;
using Android.Provider;

namespace DryForest.Storage
{
    public class FolderPickerHelper
    {
        public static void HandleStorageItemPick(Activity activity, int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == FolderPicker.BROWSE_REQUEST_CODE)
            {
                if (resultCode == Result.Ok)
                {
                    try
                    {
                        var androidUri = data.Data;
                        var takeFlags = data.Flags & (ActivityFlags.GrantReadUriPermission | ActivityFlags.GrantWriteUriPermission);
                        activity.ContentResolver.TakePersistableUriPermission(androidUri, takeFlags);
                        var folderUri = DocumentsContract.BuildDocumentUriUsingTree(androidUri, DocumentsContract.GetTreeDocumentId(androidUri));
                        OnFolderPicked(folderUri);
                    }
                    catch (Exception ex)
                    {
                        OnFolderPickCanceled();
                    }
                }
                else
                {
                    OnFolderPickCanceled();
                }
            }
        }

        public static event Action<Android.Net.Uri> FolderPicked;
        public static event Action FolderPickCanceled;

        private static void OnFolderPicked(Android.Net.Uri androidUri)
        {
            FolderPicked?.Invoke(androidUri);
        }

        private static void OnFolderPickCanceled()
        {
            FolderPickCanceled?.Invoke();
        }
    }
}
