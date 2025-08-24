using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SambaClient.App.Messages;
using SambaClient.Core.DTOs;
using SambaClient.Core.DTOs.Requests;
using SambaClient.Core.Entities;
using SambaClient.Infrastructure.Services.Interfaces;

namespace SambaClient.App.ViewModels;

public partial class AddConnectionDialogViewModel : ViewModelBase
{
    private readonly ISmbConnectionManager _connectionManager;

    [ObservableProperty]
    private string connectionName = string.Empty;

    [ObservableProperty]
    private string connectionHost = string.Empty;
    
    [ObservableProperty]
    private string shareName = string.Empty;

    [ObservableProperty]
    private string username = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isTestInProgress;

    [ObservableProperty]
    private bool showTestResult;

    [ObservableProperty]
    private bool isTestSuccess;

    [ObservableProperty]
    private string testResultMessage = string.Empty;

    [ObservableProperty]
    private IBrush testResultColor = Brushes.Black;

    [ObservableProperty]
    private double testConnectionTime;

    [ObservableProperty]
    private bool canTestConnection;

    [ObservableProperty]
    private bool canSave;

    public SmbServerConnection? SavedConnection { get; private set; }

    public AddConnectionDialogViewModel() : this(null!) { }

    public AddConnectionDialogViewModel(ISmbConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;

        PropertyChanged += OnPropertyChanged;
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateCanExecuteStates();
    }

    private void UpdateCanExecuteStates()
    {
        CanTestConnection = !string.IsNullOrWhiteSpace(ConnectionHost) &&
                            !IsTestInProgress;

        CanSave = !string.IsNullOrWhiteSpace(ConnectionName) &&
                  !string.IsNullOrWhiteSpace(ConnectionHost) &&
                  !string.IsNullOrWhiteSpace(ShareName);

        TestConnectionCommand.NotifyCanExecuteChanged();
        SaveConnectionCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTestInProgress = true;
        ShowTestResult = false;

        try
        {
            var request = new TestConnectionRequest()
            {
                Host = ConnectionHost.Trim(),
                Username = Username.Trim(),
                Password = Password
            };

            var response = await _connectionManager.TestConnectionAsync(request, CancellationToken.None);

            ShowTestResult = true;
            IsTestSuccess = response.IsSuccess;

            if (response.IsSuccess)
            {
                TestResultMessage = "Connection test successful!";
                TestResultColor = Brushes.Green;

                if (response.Shares?.Any() == true)
                {
                    TestResultMessage +=
                        $" Found {response.Shares.Count} share(s): {string.Join(", ", response.Shares)}";
                }
            }
            else
            {
                TestResultMessage = $"Connection failed: {response.ErrorMessage}";
                TestResultColor = Brushes.Red;
                TestConnectionTime = 0;
            }
        }
        catch (Exception ex)
        {
            ShowTestResult = true;
            IsTestSuccess = false;
            TestResultMessage = $"Test failed: {ex.Message}";
            TestResultColor = Brushes.Red;
            TestConnectionTime = 0;
        }
        finally
        {
            IsTestInProgress = false;
            UpdateCanExecuteStates();
        }
    }

    [RelayCommand]
    private async Task SaveConnectionAsync()
    {
        try
        {
            var request = new CreateConnectionRequest()
            {
                Name = ConnectionName.Trim(),
                Host = ConnectionHost.Trim(),
                ShareName = ShareName.Trim(),
                Username = Username.Trim(),
                Password = Password
            };

            var connection = await _connectionManager.AddNewConnectionAsync(request, CancellationToken.None);

            SavedConnection = connection;
            WeakReferenceMessenger.Default.Send(new AddConnectionCloseMessage(SavedConnection));
        }
        catch (Exception ex)
        {
            ShowTestResult = true;
            IsTestSuccess = false;
            TestResultMessage = $"Save failed: {ex.Message}";
            TestResultColor = Brushes.Red;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        WeakReferenceMessenger.Default.Send(new AddConnectionCloseMessage(null));
    }
}