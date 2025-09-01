using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SambaClient.App.ViewModels;

public class ViewModelBase : ObservableObject
{
    // protected CancellationTokenSource _cts = new();
    //
    // protected CancellationToken GetNewCancellationToken()
    // {
    //     try
    //     {
    //         _cts.Cancel();
    //     }
    //     catch { }
    //     _cts.Dispose();
    //     _cts = new CancellationTokenSource();
    //     return _cts.Token;
    // }
}