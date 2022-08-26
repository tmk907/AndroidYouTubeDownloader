using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DryForest.Storage
{
    public interface IStorageItem
    {
        bool IsFile { get; }
        bool IsFolder { get; }
        string Uri { get; }
        string Name { get; }

        Task<IStorageItem> CreateFileAsync(string name, string mimeType);
        Task<IStorageItem> CreateFolderAsync(string name);
        Task Delete();
        Task<IEnumerable<IStorageItem>> GetFilesAsync();
        Task<IEnumerable<IStorageItem>> GetFoldersAsync();
        Task<IStorageItem> GetItemAsync(string name);
        bool HasFile(string displayName);
        Task<Stream> OpenStreamAsync(FileAccess fileAccess = FileAccess.Read);
        Task<bool> Exists();
    }
}
