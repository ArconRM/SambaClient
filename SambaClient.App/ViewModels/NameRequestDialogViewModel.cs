using System;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SambaClient.App.Messages;

namespace SambaClient.App.ViewModels;

public partial class NameRequestDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private string fileName = string.Empty;

    [RelayCommand]
    private void SaveName()
    {
        WeakReferenceMessenger.Default.Send(new NameRequestCloseMessage(FileName));
    }

    [RelayCommand]
    private void Cancel()
    {
        WeakReferenceMessenger.Default.Send(new NameRequestCloseMessage(null));
    }
}