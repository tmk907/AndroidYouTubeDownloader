using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AndroidX.DocumentFile.Provider;
using Plugin.CurrentActivity;

namespace DryForest.Storage
{
    public partial class StorageItem : IStorageItem
    {
        private readonly Android.Net.Uri _contentUri;

        private DocumentFile thisDocumentFile =>
            DocumentFile.FromTreeUri(CrossCurrentActivity.Current.AppContext, _contentUri);

        public StorageItem(string contentUri)
        {
            _contentUri = Android.Net.Uri.Parse(contentUri);
        }

        public StorageItem(Android.Net.Uri folderTreeUri)
        {
            _contentUri = folderTreeUri;
        }

        public bool IsFile => thisDocumentFile.IsFile;

        public bool IsFolder => thisDocumentFile.IsDirectory;
        public string Uri => thisDocumentFile.Uri.ToString();

        public string Name => thisDocumentFile.Name;

        public Task<IEnumerable<IStorageItem>> GetFilesAsync()
        {
            IEnumerable<IStorageItem> items = thisDocumentFile.ListFiles()
                .Where(x => x.IsFile)
                .Select(x => new StorageItem(x.Uri));
            return Task.FromResult(items);
        }

        public Task<IEnumerable<IStorageItem>> GetFoldersAsync()
        {
            IEnumerable<IStorageItem> items = thisDocumentFile.ListFiles()
                .Where(x => x.IsDirectory)
                .Select(x => new StorageItem(x.Uri));
            return Task.FromResult(items);
        }

        public bool HasFile(string displayName)
        {
            return thisDocumentFile.FindFile(displayName) != null;
        }

        public Task<IStorageItem> GetItemAsync(string name)
        {
            var item = thisDocumentFile.FindFile(name);
            if (item == null)
            {
                throw new ArgumentException($"File or folder {name} does not exists");
            }
            IStorageItem storageItem = new StorageItem(item.Uri);
            return Task.FromResult(storageItem);
        }

        public Task<IStorageItem> CreateFolderAsync(string name)
        {
            var folder = thisDocumentFile.CreateDirectory(name);
            IStorageItem item = new StorageItem(folder.Uri);
            return Task.FromResult(item);
        }

        /// <summary>
        /// file extension is based on mimeType
        /// </summary>
        /// <param name="name"></param>
        /// <param name="mimeType"></param>
        public Task<IStorageItem> CreateFileAsync(string name, string mimeType)
        {
            var file = thisDocumentFile.CreateFile(mimeType, name);
            IStorageItem item = new StorageItem(file.Uri);
            return Task.FromResult(item);
        }

        public Task Delete()
        {
            thisDocumentFile.Delete();
            return Task.CompletedTask;
        }

        public Task<Stream> OpenStreamAsync(FileAccess fileAccess = FileAccess.Read)
        {
            if (IsFolder)
            {
                throw new Exception("Cannot open stream from folder");
            }

            var resolver = CrossCurrentActivity.Current.Activity.ContentResolver;
            if (fileAccess == FileAccess.Read)
            {
                return Task.FromResult(resolver.OpenInputStream(_contentUri));
            }
            else
            {
                return Task.FromResult(resolver.OpenOutputStream(_contentUri));
            }
        }

        public Task<bool> Exists()
        {
            return Task.FromResult(thisDocumentFile.Exists());
        }
    }
}
