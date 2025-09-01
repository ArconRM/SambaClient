using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using SambaClient.App.Services.Interfaces;
using SambaClient.App.ViewModels;
using SambaClient.App.Views;
using SambaClient.Core.Entities;

namespace SambaClient.App.Services;

public class FileDialogService : IFileDialogService
{
    private readonly IServiceProvider _serviceProvider;

    public FileDialogService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<IStorageFile?> OpenFileDialogAsync(
        string title,
        CancellationToken token,
        bool allowMultiple = false)
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

    public async Task<IStorageFile?> OpenSaveFileDialogAsync(
        string title,
        WellKnownFolder suggestedStartLocation,
        string suggestedFileName,
        string extension,
        CancellationToken token)
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

    public async Task<string?> OpenFolderDialogAsync(
        string title,
        CancellationToken token)
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

    public async Task<string?> OpenSelectFolderDialogAsync(
        SmbServerConnection connection,
        bool isConnected,
        string currentPath,
        CancellationToken token)
    {
        var viewModel = _serviceProvider.GetRequiredService<SelectFolderWindowViewModel>();

        viewModel.CurrentSmbServerConnection = connection;
        viewModel.IsConnected = isConnected;
        viewModel.CurrentPath = currentPath;

        if (isConnected)
        {
            await viewModel.LoadFilesAsync(token);
        }

        var window = new SelectFolderWindow
        {
            DataContext = viewModel
        };
        viewModel.SetParentWindow(window);

        return await window.ShowDialog<string?>(GetMainWindow());
    }

    private Window GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            return desktop.MainWindow!;

        throw new InvalidOperationException("Could not find main window");
    }
}