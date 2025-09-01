using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SambaClient.Core.DTOs.Requests;
using SambaClient.Core.Entities;
using SambaClient.Infrastructure.Services.Interfaces;

namespace SambaClient.App.ViewModels.Base;

public partial class BaseConnectionManagerViewModel : ViewModelBase
{
    private readonly ISmbConnectionManager _connectionManager;

    public ObservableCollection<SmbServerConnection> SmbServerConnections { get; } = new();

    [ObservableProperty]
    private SmbServerConnection? currentSmbServerConnection;

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

    public BaseConnectionManagerViewModel() : this(null!) { }

    public BaseConnectionManagerViewModel(ISmbConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
        
        _ = LoadConnectionsAsync(CancellationToken.None);
    }
    
    protected void UpdateCanConnect()
    {
        CanConnect = CurrentSmbServerConnection != null && !IsConnected;
        ConnectToServerCommand.NotifyCanExecuteChanged();
    }

    protected async Task LoadConnectionsAsync(CancellationToken token)
    {
        try
        {
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
    protected async Task DeleteConnectionAsync(CancellationToken token)
    {
        if (CurrentSmbServerConnection is null) return;

        try
        {
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
    protected async Task ConnectToServerAsync(CancellationToken token)
    {
        if (CurrentSmbServerConnection is null) return;

        IsLoading = true;
        StatusMessage = "Connecting...";

        try
        {
            var response = await _connectionManager.ConnectAsync(CurrentSmbServerConnection.Uuid, token);

            if (response.IsSuccess)
            {
                IsConnected = true;
                StatusMessage = "";
            }
            else
            {
                StatusMessage = response.ErrorMessage;
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
    protected void DisconnectFromServer()
    {
        IsConnected = false;
        StatusMessage = "";

        UpdateCanConnect();
    }

    [RelayCommand]
    protected async Task TestConnectionAsync(CancellationToken token)
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
}