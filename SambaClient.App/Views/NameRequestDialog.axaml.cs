using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using SambaClient.App.Messages;

namespace SambaClient.App.Views;

public partial class NameRequestDialog : Window
{
    public NameRequestDialog()
    {
        InitializeComponent();
        
        WeakReferenceMessenger.Default.Register<NameRequestDialog, NameRequestCloseMessage>(this,
            static (window, message) => { window.Close(message.FolderName); });
    }
}