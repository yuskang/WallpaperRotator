using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using WallpaperRotator.Application.Coordinators;
using WallpaperRotator.Application.Services;
using WallpaperRotator.Core.Interfaces;
using WallpaperRotator.Infrastructure;
using WallpaperRotator.Infrastructure.Startup;
using WallpaperRotator.Infrastructure.Storage;
using WallpaperRotator.Infrastructure.Windows;
using WallpaperRotator.Presentation.ViewModels;
using WallpaperRotator.Views;

namespace WallpaperRotator;

/// <summary>
/// 應用程式主類別
/// </summary>
public partial class App : System.Windows.Application
{
    private IHost? _host;
    private TrayIconWindow? _trayIconWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 設定 Serilog
        ConfigureLogging();

        // 建立 Host
        _host = CreateHostBuilder(e.Args).Build();

        // 啟動服務
        await _host.StartAsync();

        // 檢查是否首次運行
        var configStore = _host.Services.GetRequiredService<IConfigurationStore>();
        var config = await configStore.LoadAsync();

        if (IsFirstRun(configStore))
        {
            // 顯示設定精靈
            ShowSetupWizard();
        }

        // 初始化系統托盤
        InitializeTrayIcon();

        Log.Information("WallpaperRotator started successfully");
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((context, services) =>
            {
                // Core Services
                services.AddSingleton<IEventBus, InMemoryEventBus>();

                // Infrastructure Services
                services.AddSingleton<IOrientationDetector, OrientationDetector>();
                services.AddSingleton<IWallpaperApplier, WallpaperApplier>();
                services.AddSingleton<IConfigurationStore, JsonConfigurationStore>();
                services.AddSingleton<AutoStartManager>();

                // Application Services
                services.AddSingleton<WallpaperService>();
                services.AddSingleton<AppCoordinator>();

                // Hosted Service
                services.AddHostedService(sp => sp.GetRequiredService<AppCoordinator>());

                // ViewModels
                services.AddTransient<TrayIconViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Views
                services.AddTransient<SettingsWindow>();
            });

    private static void ConfigureLogging()
    {
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WallpaperRotator", "logs", "wallpaperrotator-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console()
            .CreateLogger();
    }

    private void InitializeTrayIcon()
    {
        var viewModel = _host!.Services.GetRequiredService<TrayIconViewModel>();

        viewModel.SettingsRequested += (_, _) => ShowSettings();
        viewModel.ExitRequested += (_, _) => ExitApplication();

        _trayIconWindow = new TrayIconWindow(viewModel);
        _trayIconWindow.Show();
    }

    private void ShowSettings()
    {
        var settingsWindow = _host!.Services.GetRequiredService<SettingsWindow>();
        settingsWindow.ShowDialog();
    }

    private void ShowSetupWizard()
    {
        // TODO: 實現設定精靈
        // var wizard = _host!.Services.GetRequiredService<SetupWizardWindow>();
        // wizard.ShowDialog();
    }

    private static bool IsFirstRun(IConfigurationStore configStore)
    {
        return !File.Exists(configStore.ConfigFilePath);
    }

    private async void ExitApplication()
    {
        Log.Information("WallpaperRotator shutting down...");

        _trayIconWindow?.Close();

        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        Shutdown();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
