using System.Threading.Tasks;

namespace DryForest.Storage
{
    public interface IFolderPicker
    {
        Task<IStorageItem> PickFolderAsync();
    }
}
