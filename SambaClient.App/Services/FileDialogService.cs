using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using SambaClient.App.Services.Interfaces;

namespace SambaClient.App.Services;

public class FileDialogService : IFileDialogService
{
    public async Task<IStorageFile?> OpenFileDialogAsync(string title, bool allowMultiple = false)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
        {
            return null;
        }

        var options = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = allowMultiple
        };

        var files = await provider.OpenFilePickerAsync(options);
        return files.Count >= 1 ? files[0] : null;
    }

    public async Task<IStorageFile?> SaveFileDialogAsync(
        string title,
        WellKnownFolder suggestedStartLocation,
        string suggestedFileName,
        string extension)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
        {
            return null;
        }

        var options = new FilePickerSaveOptions
        {
            Title = title,
            SuggestedFileName = suggestedFileName,
            DefaultExtension = extension,
            SuggestedStartLocation = await provider.TryGetWellKnownFolderAsync(suggestedStartLocation)
        };

        return await provider.SaveFilePickerAsync(options);
    }
    
    public async Task<string?> OpenFolderDialogAsync(string title)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
        {
            return null;
        }

        var options = new FolderPickerOpenOptions
        {
            Title = title
        };

        var folders = await provider.OpenFolderPickerAsync(options);
        return folders.Count >= 1 ? folders[0].Path.LocalPath : null;
    }
}