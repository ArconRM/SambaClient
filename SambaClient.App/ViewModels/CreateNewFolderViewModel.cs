using System;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SambaClient.App.Messages;

namespace SambaClient.App.ViewModels;

public partial class CreateNewFolderViewModel : ViewModelBase
{
    [ObservableProperty]
    private string folderName = string.Empty;

    [RelayCommand]
    private void SaveFolder()
    {
        WeakReferenceMessenger.Default.Send(new CreateNewFolderCloseMessage(folderName));
    }

    [RelayCommand]
    private void Cancel()
    {
        WeakReferenceMessenger.Default.Send(new CreateNewFolderCloseMessage(null));
    }
}