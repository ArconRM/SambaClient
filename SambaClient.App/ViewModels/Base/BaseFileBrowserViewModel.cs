using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SambaClient.Core.Entities;
using SambaClient.Infrastructure.Services.Interfaces;

namespace SambaClient.App.ViewModels.Base;

public abstract partial class BaseFileBrowserViewModel : BaseConnectionManagerViewModel
{
    protected readonly ISmbService SmbService;
    
    public ObservableCollection<FileEntity> Files { get; } = new();

    [ObservableProperty]
    private FileEntity? selectedFile;

    [ObservableProperty]
    private string currentPath = "";

    protected string SelectedFilePath => SelectedFile != null 
        ? Path.Combine(CurrentPath, SelectedFile.FileName) 
        : string.Empty;

    protected BaseFileBrowserViewModel(): this(null!, null!) { }

    protected BaseFileBrowserViewModel(ISmbConnectionManager connectionManager, ISmbService smbService) : base(connectionManager)
    {
        SmbService = smbService;
    }

    [RelayCommand]
    public async virtual Task LoadFilesAsync(CancellationToken token)
    {
        if (!IsConnected || CurrentSmbServerConnection is null) return;

        IsLoading = true;

        try
        {
            var response = await SmbService.GetAllFilesAsync(CurrentSmbServerConnection.Uuid, CurrentPath, token);

            if (response.IsSuccess)
            {
                Files.Clear();
                foreach (var file in response.Files)
                {
                    if (file.FileName.StartsWith("."))
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
    protected async Task ConnectToServerWithLoadingFilesAsync(CancellationToken token)
    {
        await ConnectToServerAsync(token);
        await LoadFilesAsync(token);
    }

    [RelayCommand]
    protected void DisconnectFromServerWithClearing()
    {
        DisconnectFromServer();
        Files.Clear();
    }

    [RelayCommand]
    public async Task MoveToInnerFolderAsync(CancellationToken token)
    {
        if (SelectedFile is null || !SelectedFile.IsDirectory) return;

        CurrentPath = Path.Combine(CurrentPath, SelectedFile.FileName);
        await LoadFilesAsync(token);
    }

    [RelayCommand]
    public async Task MoveToParentFolderAsync(CancellationToken token)
    {
        if (string.IsNullOrEmpty(CurrentPath)) return;

        CurrentPath = Path.GetDirectoryName(CurrentPath) ?? string.Empty;
        await LoadFilesAsync(token);
    }
}