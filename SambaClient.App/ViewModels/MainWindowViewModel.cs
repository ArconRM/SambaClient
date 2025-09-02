using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using SambaClient.App.Messages;
using SambaClient.App.Services.Interfaces;
using SambaClient.App.ViewModels.Base;
using SambaClient.Core.DTOs.Requests;
using SambaClient.Core.DTOs.Responses;
using SambaClient.Infrastructure.Services.Interfaces;

namespace SambaClient.App.ViewModels;

public partial class MainWindowViewModel : BaseFileBrowserViewModel
{
    private readonly IFileDialogService _fileDialogService;

    public MainWindowViewModel() : this(
        null!,
        null!,
        null!) { }

    public MainWindowViewModel(
        IFileDialogService fileDialogService,
        ISmbConnectionManager connectionManager,
        ISmbService smbService) : base(connectionManager, smbService)
    {
        _fileDialogService = fileDialogService;
        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CurrentSmbServerConnection):
                UpdateCanConnect();
                TestConnectionCommand.NotifyCanExecuteChanged();
                DisconnectFromServerWithClearing();
                break;
            case nameof(IsConnected):
                LoadFilesCommand.NotifyCanExecuteChanged();
                UpdateConnectionStatus();
                break;
        }
    }
    
    private void UpdateConnectionStatus()
    {
        if (IsConnected && CurrentSmbServerConnection != null)
        {
            ConnectionStatus = $"Connected to {CurrentSmbServerConnection.Name}";
            ConnectionStatusColor = Brushes.Green;
        }
        else
        {
            ConnectionStatus = "Not connected";
            ConnectionStatusColor = Brushes.Red;
        }
    }

    [RelayCommand]
    private async Task ShowAddConnectionDialogAsync(CancellationToken token)
    {
        if (CurrentSmbServerConnection is not null)
            DisconnectFromServer();

        var serverConnection = await WeakReferenceMessenger.Default.Send(new AddConnectionMessage());

        if (serverConnection != null)
        {
            SmbServerConnections.Add(serverConnection);
            CurrentSmbServerConnection = serverConnection;
        }
    }

    [RelayCommand]
    private async Task DownloadFileAsync(CancellationToken token)
    {
        if (CurrentSmbServerConnection is null || SelectedFile is null || SelectedFile.IsDirectory) return;

        try
        {
            var saveFile = await _fileDialogService.OpenSaveFileDialogAsync(
                "Save file as",
                WellKnownFolder.Downloads,
                Path.GetFileNameWithoutExtension(SelectedFile.FileName),
                Path.GetExtension(SelectedFile.FileName),
                token);

            var request = new FileRequest
            {
                ConnectionUuid = CurrentSmbServerConnection.Uuid,
                RemotePath = SelectedFilePath
            };

            DownloadFileResponse response = await SmbService.DownloadFileAsync(request, token);

            if (response.IsSuccess)
            {
                using var fileStream = await saveFile.OpenWriteAsync();

                await response.Stream.CopyToAsync(fileStream, token);

                StatusMessage = $"File downloaded successfully to: {saveFile.Name}";
            }
            else
            {
                StatusMessage = $"Error downloading file: {response.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error downloading file: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task UploadFileAsync(CancellationToken token)
    {
        if (CurrentSmbServerConnection is null) return;

        try
        {
            var file = await _fileDialogService.OpenFileDialogAsync("Select a file", token);
            if (file is null) return;

            await using var stream = await file.OpenReadAsync();

            var request = new UploadFileRequest
            {
                ConnectionUuid = CurrentSmbServerConnection.Uuid,
                RemotePath = Path.Combine(CurrentPath, file.Name),
                SourceStream = stream,
                OverwriteIfExists = false,
            };

            var response = await SmbService.UploadFileAsync(request, token);

            if (response.IsSuccess)
            {
                StatusMessage = $"Uploaded new file";
                await LoadFilesAsync(token);
            }
            else
            {
                StatusMessage = $"Error uploading file: {response.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error uploading file: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CreateNewFolderAsync(CancellationToken token)
    {
        if (CurrentSmbServerConnection is null) return;

        var folderName = await WeakReferenceMessenger.Default.Send(new NameRequestMessage());

        if (string.IsNullOrEmpty(folderName)) return;

        try
        {
            var targetPath = Path.Combine(CurrentPath, folderName);

            var request = new FileRequest
            {
                ConnectionUuid = CurrentSmbServerConnection.Uuid,
                RemotePath = targetPath,
            };

            var response = await SmbService.CreateFolderAsync(request, token);

            if (response.IsSuccess)
            {
                StatusMessage = $"Created new folder: {folderName}";
                await LoadFilesAsync(token);
            }
            else
            {
                StatusMessage = $"Error creating folder: {response.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error creating folder: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RenameFileAsync(CancellationToken token)
    {
        if (CurrentSmbServerConnection is null || SelectedFile is null) return;

        var currentFileName = SelectedFile.FileName;
        var newName = await WeakReferenceMessenger.Default.Send(new NameRequestMessage(currentFileName));

        if (string.IsNullOrEmpty(newName)) return;

        try
        {
            var oldTargetPath = SelectedFilePath;
            var newTargetPath = Path.Combine(CurrentPath, newName);

            var request = new UpdateFilePathRequest
            {
                ConnectionUuid = CurrentSmbServerConnection.Uuid,
                RemotePath = oldTargetPath,
                NewRemotePath = newTargetPath,
                IsDirectory = SelectedFile.IsDirectory
            };

            var response = await SmbService.UpdateFileNameAsync(request, token);

            if (response.IsSuccess)
            {
                StatusMessage = $"Renamed {SelectedFile.FileName} -> {newName}";
                await LoadFilesAsync(token);
            }
            else
            {
                StatusMessage = $"Error renaming a file: {response.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error renaming a file: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteFileAsync(CancellationToken token)
    {
        if (CurrentSmbServerConnection is null || SelectedFile is null) return;

        try
        {
            var box = MessageBoxManager
                .GetMessageBoxStandard("Delete file", "Are you sure you would like to delete file?",
                    ButtonEnum.YesNo);

            var result = await box.ShowAsync();
            if (result != ButtonResult.Yes) return;
            
            var request = new FileRequest
            {
                ConnectionUuid = CurrentSmbServerConnection.Uuid,
                RemotePath = SelectedFilePath,
                IsDirectory = SelectedFile.IsDirectory
            };

            var response = await SmbService.DeleteFileAsync(request, token);

            if (response.IsSuccess)
            {
                StatusMessage = $"Deleted {SelectedFile.FileName}";
                await LoadFilesAsync(token);
            }
            else
            {
                StatusMessage = $"Error deleting file: {response.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error deleting file: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task MoveFileAsync(CancellationToken token)
    {
        if (CurrentSmbServerConnection is null || SelectedFile is null) return;
        
        var newPath = await _fileDialogService.OpenSelectFolderDialogAsync(
            CurrentSmbServerConnection,
            IsConnected,
            CurrentPath,
            token);
        if (newPath is null) return;

        try
        {
            var request = new UpdateFilePathRequest
            {
                ConnectionUuid = CurrentSmbServerConnection.Uuid,
                RemotePath = SelectedFilePath,
                NewRemotePath = Path.Combine(newPath, SelectedFile.FileName),
                IsDirectory = SelectedFile.IsDirectory
            };

            var response = await SmbService.MoveFileAsync(request, token);
            if (response.IsSuccess)
            {
                StatusMessage = $"Moved {SelectedFile.FileName} -> {newPath}";
                await LoadFilesAsync(token);
            }
            else
            {
                StatusMessage = $"Error moving file: {response.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error moving file: {ex.Message}";
        }
    }

    public static readonly IValueConverter FileTypeConverter =
        new FuncValueConverter<bool, string>(isDirectory => isDirectory ? "Folder" : "File");

    public static readonly IValueConverter FileSizeConverter =
        new FuncValueConverter<long, string>(size => 
            size == 0 
                ? "-" 
                : $"{((double)size / (1000 * 1000)):0.00} MB");
}