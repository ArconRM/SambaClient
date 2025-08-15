using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SambaClient.App.Messages;
using SambaClient.Core.DTOs.Requests;
using SambaClient.Core.Entities;
using SambaClient.Infrastructure.Services.Interfaces;

namespace SambaClient.App.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
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

    public MainWindowViewModel() : this(null!,
        null!) { }

    public MainWindowViewModel(
        ISmbConnectionManager connectionManager,
        ISmbService smbService)
    {
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

    private async Task LoadConnections()
    {
        try
        {
            var response = await _connectionManager.LoadConnectionsAsync(CancellationToken.None);
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
            DisconnectFromServer();
            await _connectionManager.RemoveConnectionAsync(CurrentSmbServerConnection.Uuid, CancellationToken.None);

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
            var testResponse =
                await _connectionManager.ConnectAsync(CurrentSmbServerConnection.Uuid, CancellationToken.None);

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
            var request = new TestConnectionRequest
            {
                Host = CurrentSmbServerConnection.Host,
                Username = CurrentSmbServerConnection.Username,
                Password = CurrentSmbServerConnection.Password
            };

            var response = await _connectionManager.TestConnectionAsync(request, CancellationToken.None);

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
        StatusMessage = "Loading files...";

        try
        {
            var response = await _smbService.GetAllFilesAsync(CurrentSmbServerConnection.Uuid, CurrentPath, CancellationToken.None);

            if (response.IsSuccess)
            {
                Files.Clear();
                foreach (var file in response.Files)
                {
                    if (file.FileName.StartsWith("."))
                        continue;
                    
                    Files.Add(file);
                }

                StatusMessage = $"Loaded {Files.Count} items";
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

        CurrentPath = Path.Combine(CurrentPath, SelectedFile.FileName);
        await RefreshFilesAsync();
    }

    [RelayCommand]
    private async Task MoveToParentFolderAsync()
    {
        if (string.IsNullOrEmpty(CurrentPath)) return;

        CurrentPath = Path.GetDirectoryName(CurrentPath);
        await RefreshFilesAsync();
    }
    
    public static readonly IValueConverter FileTypeConverter =
        new FuncValueConverter<bool, string>(isDirectory => isDirectory ? "Folder" : "File");
}