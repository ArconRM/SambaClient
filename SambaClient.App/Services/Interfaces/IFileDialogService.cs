using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using SambaClient.Core.Entities;

namespace SambaClient.App.Services.Interfaces;

public interface IFileDialogService
{
    Task<IStorageFile?> OpenFileDialogAsync(
        string title,
        CancellationToken token,
        bool allowMultiple = false);
    
    Task<IStorageFile?> OpenSaveFileDialogAsync(
        string title,
        WellKnownFolder suggestedStartLocation,
        string suggestedFileName,
        string extension,
        CancellationToken token);

    Task<string?> OpenFolderDialogAsync(
        string title,
        CancellationToken token);
    
    Task<string?> OpenSelectFolderDialogAsync(
        SmbServerConnection connection,
        bool isConnected,
        string currentPath,
        CancellationToken token);
}
