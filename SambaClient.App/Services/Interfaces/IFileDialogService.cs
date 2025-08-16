using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace SambaClient.App.Services.Interfaces;

public interface IFileDialogService
{
    Task<IStorageFile?> OpenFileDialogAsync(string title, bool allowMultiple = false);
    
    Task<IStorageFile?> SaveFileDialogAsync(
        string title,
        WellKnownFolder suggestedStartLocation,
        string suggestedFileName,
        string extension);
}
