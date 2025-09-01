using System;
using System.Threading;
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
            var vm = App.Services.GetRequiredService<AddConnectionDialogViewModel>();

            var dialog = new AddConnectionDialog
            {
                DataContext = vm
            };
            m.Reply(dialog.ShowDialog<SmbServerConnection?>(w));
        });
        
        WeakReferenceMessenger.Default.Register<MainWindow, NameRequestMessage>(this, (w, m) =>
        {
            var vm = App.Services.GetRequiredService<NameRequestDialogViewModel>();
            vm.FileName = m.DefaultName;

            var dialog = new NameRequestDialog
            {
                DataContext = vm
            };
            m.Reply(dialog.ShowDialog<string?>(w));
        });
        
        // WeakReferenceMessenger.Default.Register<MainWindow, SelectFolderMessage>(this, (w, m) =>
        // {
        //     var vm = App.Services.GetRequiredService<SelectFolderWindowViewModel>();
        //
        //     var dialog = new SelectFolderWindow
        //     {
        //         DataContext = vm
        //     };
        //
        //     m.Reply(dialog.ShowDialog<string?>(w));
        // });
    }

    private async void OnDataGridDoubleTapped(object sender, Avalonia.Input.TappedEventArgs e)
    {
        var dataGrid = sender as DataGrid;
        var vm = (MainWindowViewModel)DataContext;

        var selectedItem = dataGrid?.SelectedItem;
        if (selectedItem != null && vm != null)
        {
            await vm.MoveToInnerFolderAsync(CancellationToken.None);
        }
    }
}