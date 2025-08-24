using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SambaClient.App.Services;
using SambaClient.App.Services.Interfaces;
using SambaClient.App.ViewModels;
using SambaClient.App.Views;
using SambaClient.Infrastructure.Services;
using SambaClient.Infrastructure.Services.Interfaces;

namespace SambaClient.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        DisableAvaloniaDataAnnotationValidation();
        
        var services = new ServiceCollection();
        var mainWindow = new MainWindow();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {

            var mainViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();

            mainWindow.DataContext = mainViewModel;
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
    
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IFileDialogService, FileDialogService>();

        services.AddSingleton<ISmbClientProvider, SmbClientProvider>();
        services.AddSingleton<ISmbConnectionManager, ConnectionManager>();
        services.AddSingleton<ISmbService, SmbService>();
        
        services.AddTransient<MainWindowViewModel>(provider =>
        {
            var fileDialogService = provider.GetRequiredService<IFileDialogService>();
            var connectionManager = provider.GetRequiredService<ISmbConnectionManager>();
            var smbService = provider.GetRequiredService<ISmbService>();
            return new MainWindowViewModel(fileDialogService, connectionManager, smbService);
        });
        
        services.AddTransient<AddConnectionDialogViewModel>(provider =>
        {
            var connectionManager = provider.GetRequiredService<ISmbConnectionManager>();
            return new AddConnectionDialogViewModel(connectionManager);
        });

        services.AddTransient<NameRequestDialogViewModel>(_ => new NameRequestDialogViewModel());
    }

    public static ServiceProvider Services => ((App)Current!)._serviceProvider!;
}