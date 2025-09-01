using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SambaClient.App.ViewModels;
using SambaClient.App.ViewModels.Base;

namespace SambaClient.App.Views;

public partial class SelectFolderWindow : Window
{
    public SelectFolderWindow()
    {
        InitializeComponent();
    }
    
    private async void OnDataGridDoubleTapped(object sender, Avalonia.Input.TappedEventArgs e)
    {
        var dataGrid = sender as DataGrid;
        var vm = (SelectFolderWindowViewModel)DataContext;

        var selectedItem = dataGrid?.SelectedItem;
        if (selectedItem != null && vm != null)
        {
            await vm.MoveToInnerFolderAsync(CancellationToken.None);
        }
    }
}