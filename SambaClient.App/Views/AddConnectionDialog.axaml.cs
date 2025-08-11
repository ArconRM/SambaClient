using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.Messaging;
using SambaClient.App.Messages;
using SambaClient.App.ViewModels;

namespace SambaClient.App.Views;

public partial class AddConnectionDialog : Window
{
    public AddConnectionDialog()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<AddConnectionDialog, AddConnectionCloseMessage>(this,
            static (window, message) => { window.Close(message.SmbServerConnection); });
    }
}