using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using SambaClient.App.Messages;

namespace SambaClient.App.Views;

public partial class NameRequesDialog : Window
{
    public NameRequesDialog()
    {
        InitializeComponent();
        
        WeakReferenceMessenger.Default.Register<NameRequesDialog, NameRequestCloseMessage>(this,
            static (window, message) => { window.Close(message.FolderName); });
    }
}