using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SambaClient.App.Messages;
using SambaClient.App.ViewModels;
using SambaClient.Core.Entities;

namespace SambaClient.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.Register<MainWindow, AddConnectionMessage>(this, (w, m) =>
        {
            var vm = App.Services.GetRequiredService<AddConnectionViewModel>();

            var dialog = new AddConnectionDialog()
            {
                DataContext = vm
            };
            m.Reply(dialog.ShowDialog<SmbServerConnection?>(w));
        });
    }
}