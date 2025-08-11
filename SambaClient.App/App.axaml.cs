using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using SambaClient.App.ViewModels;
using SambaClient.App.Views;
using SambaClient.Core.Services;
using SambaClient.Core.Services.Interfaces;
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
        ConfigureServices(services, mainWindow);
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
    
    private static void ConfigureServices(IServiceCollection services, Window mainWindow)
    {
        // Core services
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        // services.AddSingleton<ISmbService, SmbService>();
        
        // Infrastructure services
        services.AddSingleton<ISmbClientProvider, SmbClientProvider>();
        
        // ViewModels
        services.AddTransient<MainWindowViewModel>(provider =>
        {
            var connectionManager = provider.GetRequiredService<IConnectionManager>();
            // var smbService = provider.GetRequiredService<ISmbService>();
            return new MainWindowViewModel(connectionManager, mainWindow);
        });
        
        services.AddTransient<AddConnectionViewModel>(provider =>
        {
            var connectionManager = provider.GetRequiredService<IConnectionManager>();
            return new AddConnectionViewModel(connectionManager);
        });

    }

    public static ServiceProvider Services => ((App)Current!)._serviceProvider!;
}