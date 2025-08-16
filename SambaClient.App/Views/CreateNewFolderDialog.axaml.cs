using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using SambaClient.App.Messages;

namespace SambaClient.App.Views;

public partial class CreateNewFolderDialog : Window
{
    public CreateNewFolderDialog()
    {
        InitializeComponent();
        
        WeakReferenceMessenger.Default.Register<CreateNewFolderDialog, CreateNewFolderCloseMessage>(this,
            static (window, message) => { window.Close(message.FolderName); });
    }
}