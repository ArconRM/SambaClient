using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SambaClient.App.Messages;
using SambaClient.App.Services.Interfaces;
using SambaClient.Core.DTOs.Requests;
using SambaClient.Core.DTOs.Responses;
using SambaClient.Core.Entities;
using SambaClient.Infrastructure.Services.Interfaces;

namespace SambaClient.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private CancellationTokenSource _cts = new();

    private readonly IFileDialogService _fileDialogService;

    private readonly ISmbConnectionManager _connectionManager;
    private readonly ISmbService _smbService;

    public ObservableCollection<SmbServerConnection> SmbServerConnections { get; } = new();
    public ObservableCollection<FileEntity> Files { get; } = new();

    [ObservableProperty]
    private SmbServerConnection? currentSmbServerConnection;

    [ObservableProperty]
    private FileEntity? selectedFile;

    [ObservableProperty]
    private string currentPath = "";

    private string selectedFilePath => Path.Combine(CurrentPath, SelectedFile.FileName);

    [ObservableProperty]
    private string connectionStatus = "Not connected";

    [ObservableProperty]
    private IBrush connectionStatusColor = Brushes.Red;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isConnected;

    [ObservableProperty]
    private bool canConnect;

    [ObservableProperty]
    private bool isLoading;

    public MainWindowViewModel() : this(
        null!,
        null!,
        null!) { }

    public MainWindowViewModel(
        IFileDialogService fileDialogService,
        ISmbConnectionManager connectionManager,
        ISmbService smbService)
    {
        _fileDialogService = fileDialogService;
        _connectionManager = connectionManager;
        _smbService = smbService;

        PropertyChanged += OnPropertyChanged;

        _ = LoadConnections();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CurrentSmbServerConnection):
                UpdateCanConnect();
                TestConnectionCommand.NotifyCanExecuteChanged();
                DisconnectFromServer();
                break;
            case nameof(IsConnected):
                RefreshFilesCommand.NotifyCanExecuteChanged();
                UpdateConnectionStatus();
                break;
        }
    }

    private void UpdateCanConnect()
    {
        CanConnect = CurrentSmbServerConnection != null && !IsConnected;
        ConnectToServerCommand.NotifyCanExecuteChanged();
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

    private CancellationToken GetNewCancellationToken()
    {
        try
        {
            _cts.Cancel();
        }
        catch { }

        _cts.Dispose();
        _cts = new CancellationTokenSource();
        return _cts.Token;
    }

    private async Task LoadConnections()
    {
        try
        {
            var token = GetNewCancellationToken();

            var response = await _connectionManager.LoadConnectionsAsync(token);
            SmbServerConnections.Clear();
            foreach (var connection in response)
            {
                SmbServerConnections.Add(connection);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load connections: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ShowAddConnectionDialogAsync()
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
    private async Task DeleteConnectionAsync()
    {
        if (CurrentSmbServerConnection is null) return;

        try
        {
            var token = GetNewCancellationToken();

            DisconnectFromServer();
            await _connectionManager.RemoveConnectionAsync(CurrentSmbServerConnection.Uuid, token);

            SmbServerConnections.Remove(CurrentSmbServerConnection);
            CurrentSmbServerConnection = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Deletion failed: {ex.Message}";
            IsConnected = false;
        }
    }

    [RelayCommand]
    private async Task ConnectToServerAsync()
    {
        if (CurrentSmbServerConnection is null) return;

        IsLoading = true;
        StatusMessage = "Connecting...";

        try
        {
            var token = GetNewCancellationToken();

            var testResponse = await _connectionManager.ConnectAsync(CurrentSmbServerConnection.Uuid, token);

            if (testResponse.IsSuccess)
            {
                IsConnected = true;
                StatusMessage = "Connected successfully";

                await RefreshFilesAsync();
            }
            else
            {
                StatusMessage = testResponse.ErrorMessage;
                IsConnected = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection failed: {ex.Message}";
            IsConnected = false;
        }
        finally
        {
            IsLoading = false;
            UpdateCanConnect();
        }
    }

    [RelayCommand]
    private void DisconnectFromServer()
    {
        IsConnected = false;
        Files.Clear();
        StatusMessage = "Disconnected";

        UpdateCanConnect();
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (CurrentSmbServerConnection is null) return;

        StatusMessage = "Testing connection...";
        IsLoading = true;

        try
        {
            var token = GetNewCancellationToken();

            var request = new TestConnectionRequest
            {
                Host = CurrentSmbServerConnection.Host,
                Username = CurrentSmbServerConnection.Username,
                Password = CurrentSmbServerConnection.Password
            };

            var response = await _connectionManager.TestConnectionAsync(request, token);

            if (response.IsSuccess)
            {
                StatusMessage = $"Connection test successful!";
            }
            else
            {
                StatusMessage = $"Connection test failed: {response.ErrorMessage}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Connection test error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshFilesAsync()
    {
        if (!IsConnected || CurrentSmbServerConnection is null) return;

        IsLoading = true;

        try
        {
            var token = GetNewCancellationToken();

            var response = await _smbService.GetAllFilesAsync(CurrentSmbServerConnection.Uuid, CurrentPath, token);

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
    public async Task MoveToInnerFolderAsync()
    {
        if (SelectedFile is null || !SelectedFile.IsDirectory) return;

        CurrentPath = selectedFilePath;
        await RefreshFilesAsync();
    }

    [RelayCommand]
    private async Task MoveToParentFolderAsync()
    {
        if (string.IsNullOrEmpty(CurrentPath)) return;

        CurrentPath = Path.GetDirectoryName(CurrentPath);
        await RefreshFilesAsync();
    }

    [RelayCommand]
    private async Task DownloadFileAsync()
    {
        if (CurrentSmbServerConnection is null || SelectedFile is null || SelectedFile.IsDirectory) return;

        try
        {
            var saveFile = await _fileDialogService.SaveFileDialogAsync(
                "Save file as",
                WellKnownFolder.Downloads,
                Path.GetFileNameWithoutExtension(SelectedFile.FileName),
                Path.GetExtension(SelectedFile.FileName));

            var token = GetNewCancellationToken();

            var request = new FileRequest
            {
                ConnectionUuid = CurrentSmbServerConnection.Uuid,
                TargetRemotePath = selectedFilePath
            };

            DownloadFileResponse response = await _smbService.DownloadFileAsync(request, token);

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
    private async Task UploadFileAsync()
    {
        if (CurrentSmbServerConnection is null) return;

        try
        {
            var token = GetNewCancellationToken();

            var file = await _fileDialogService.OpenFileDialogAsync("Select a file");
            if (file is null) return;

            await using var stream = await file.OpenReadAsync();

            var request = new UploadFileRequest
            {
                ConnectionUuid = CurrentSmbServerConnection.Uuid,
                TargetRemotePath = Path.Combine(CurrentPath, file.Name),
                SourceStream = stream,
                OverwriteIfExists = false,
            };

            var response = await _smbService.UploadFileAsync(request, token);

            if (response.IsSuccess)
            {
                StatusMessage = $"Uploaded new file to share";
                await RefreshFilesAsync();
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
    private async Task CreateNewFolderAsync()
    {
        if (CurrentSmbServerConnection is null) return;

        var folderName = await WeakReferenceMessenger.Default.Send(new NameRequestMessage());

        if (string.IsNullOrEmpty(folderName)) return;

        try
        {
            var token = GetNewCancellationToken();

            var targetPath = Path.Combine(CurrentPath, folderName);

            var request = new FileRequest
            {
                ConnectionUuid = CurrentSmbServerConnection.Uuid,
                TargetRemotePath = targetPath,
            };

            var response = await _smbService.CreateFolderAsync(request, token);

            if (response.IsSuccess)
            {
                StatusMessage = $"Created new folder: {folderName}";
                await RefreshFilesAsync();
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
    private async Task RenameFileAsync()
    {
        if (CurrentSmbServerConnection is null || SelectedFile is null) return;

        var currentFileName = SelectedFile.FileName;
        var newName = await WeakReferenceMessenger.Default.Send(new NameRequestMessage(currentFileName));
        
        if (string.IsNullOrEmpty(newName)) return;

        try
        {
            var token = GetNewCancellationToken();
            
            var oldTargetPath = Path.Combine(CurrentPath, SelectedFile.FileName);
            var newTargetPath = Path.Combine(CurrentPath, newName);

            var request = new UpdateFileNameRequest
            {
                ConnectionUuid = CurrentSmbServerConnection.Uuid,
                TargetRemotePath = oldTargetPath,
                NewRemoteTargetPath = newTargetPath
            };
            
            var response = await _smbService.UpdateFileNameAsync(request, token);

            if (response.IsSuccess)
            {
                StatusMessage = $"Renamed file: {SelectedFile.FileName} -> {newName}";
                await RefreshFilesAsync();
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
    private async Task DeleteFileAsync()
    {
        if (CurrentSmbServerConnection is null || SelectedFile is null) return;

        try
        {
            var token = GetNewCancellationToken();

            var request = new FileRequest
            {
                ConnectionUuid = CurrentSmbServerConnection.Uuid,
                TargetRemotePath = selectedFilePath
            };

            var response = await _smbService.DeleteFileAsync(request, token);

            if (response.IsSuccess)
            {
                StatusMessage = $"Deleted file";
                await RefreshFilesAsync();
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

    public static readonly IValueConverter FileTypeConverter =
        new FuncValueConverter<bool, string>(isDirectory => isDirectory ? "Folder" : "File");
}