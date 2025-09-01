using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SambaClient.App.Messages;
using SambaClient.App.ViewModels.Base; 
using SambaClient.Core.Entities;
using SambaClient.Infrastructure.Services.Interfaces;

namespace SambaClient.App.ViewModels;

public partial class SelectFolderWindowViewModel : BaseFileBrowserViewModel
{
    private Window? _parentWindow;

    public SelectFolderWindowViewModel(
        ISmbConnectionManager connectionManager,
        ISmbService smbService) : base(connectionManager, smbService) { }

    public void SetParentWindow(Window window)
    {
        _parentWindow = window;
    }

    public async override Task LoadFilesAsync(CancellationToken token)
    {
        IsLoading = true;

        try
        {
            var response = await SmbService.GetAllFilesAsync(CurrentSmbServerConnection.Uuid, CurrentPath, token);

            if (response.IsSuccess)
            {
                Files.Clear();
                foreach (var file in response.Files)
                {
                    if (file.FileName.StartsWith(".") || !file.IsDirectory)
                        continue;

                    Files.Add(file);
                }
            }
            else
            {
                StatusMessage = $"Failed to load files: {response.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading files: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectFolder()
    {
        var selectedPath = SelectedFile?.IsDirectory == true
            ? Path.Combine(CurrentPath, SelectedFile.FileName)
            : CurrentPath;

        _parentWindow?.Close(selectedPath);
    }

    [RelayCommand]
    private void Cancel()
    {
        _parentWindow?.Close(null);
    }
}