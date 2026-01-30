using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WallpaperRotator.Core.Enums;
using WallpaperRotator.Core.Events;
using WallpaperRotator.Core.Interfaces;
using WallpaperRotator.Application.Services;

namespace WallpaperRotator.Application.Coordinators;

/// <summary>
/// 應用協調器 - 管理服務生命週期和事件協調
/// </summary>
public sealed class AppCoordinator : IHostedService, IDisposable
{
    private readonly IOrientationDetector _orientationDetector;
    private readonly WallpaperService _wallpaperService;
    private readonly IConfigurationStore _configStore;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AppCoordinator> _logger;

    private IDisposable? _orientationSubscription;
    private bool _isEnabled = true;
    private ScreenOrientation _lastOrientation = ScreenOrientation.Unknown;

    public bool IsEnabled => _isEnabled;
    public ScreenOrientation CurrentOrientation => _lastOrientation;

    public AppCoordinator(
        IOrientationDetector orientationDetector,
        WallpaperService wallpaperService,
        IConfigurationStore configStore,
        IEventBus eventBus,
        ILogger<AppCoordinator> logger)
    {
        _orientationDetector = orientationDetector;
        _wallpaperService = wallpaperService;
        _configStore = configStore;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AppCoordinator starting...");

        // 載入配置
        var config = await _configStore.LoadAsync();
        _isEnabled = config.Settings.Enabled;

        // 訂閱方向變更事件
        _orientationDetector.OrientationChanged += OnOrientationChanged;

        // 開始監控
        _orientationDetector.StartMonitoring();

        // 取得當前方向
        _lastOrientation = _orientationDetector.GetCurrentOrientation();

        // 初始套用當前方向對應的桌布
        if (_isEnabled)
        {
            await _wallpaperService.SwitchForOrientationAsync(_lastOrientation);
        }

        _logger.LogInformation(
            "AppCoordinator started. Enabled: {Enabled}, Orientation: {Orientation}",
            _isEnabled,
            _lastOrientation);
    }

    private async void OnOrientationChanged(object? sender, OrientationChangedEventArgs e)
    {
        _lastOrientation = e.CurrentOrientation;

        if (!_isEnabled)
        {
            _logger.LogDebug("Orientation changed but service is disabled");
            return;
        }

        _logger.LogInformation(
            "Orientation changed: {Previous} → {Current}",
            e.PreviousOrientation,
            e.CurrentOrientation);

        // 發布領域事件
        _eventBus.Publish(new OrientationChangedEvent(e.PreviousOrientation, e.CurrentOrientation));

        try
        {
            await _wallpaperService.SwitchForOrientationAsync(e.CurrentOrientation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch wallpaper on orientation change");
        }
    }

    /// <summary>
    /// 啟用或停用服務
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        if (_isEnabled == enabled) return;

        _isEnabled = enabled;
        _logger.LogInformation("Service {Status}", enabled ? "enabled" : "disabled");

        _eventBus.Publish(new ServiceStateChangedEvent(enabled));

        // 如果啟用，立即套用當前方向的桌布
        if (enabled)
        {
            _ = _wallpaperService.SwitchForOrientationAsync(_lastOrientation);
        }
    }

    /// <summary>
    /// 切換啟用狀態
    /// </summary>
    public void ToggleEnabled() => SetEnabled(!_isEnabled);

    /// <summary>
    /// 手動切換到指定方向
    /// </summary>
    public Task SwitchToOrientationAsync(ScreenOrientation orientation) =>
        _wallpaperService.SwitchToAsync(orientation);

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AppCoordinator stopping...");
        _orientationDetector.StopMonitoring();
        _orientationDetector.OrientationChanged -= OnOrientationChanged;
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _orientationSubscription?.Dispose();
        _orientationDetector.Dispose();
        _wallpaperService.Dispose();
    }
}
