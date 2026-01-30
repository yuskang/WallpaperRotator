using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WallpaperRotator.Core.Enums;
using WallpaperRotator.Core.Events;
using WallpaperRotator.Core.Interfaces;
using WallpaperRotator.Application.Services;

namespace WallpaperRotator.Application.Coordinators;

/// <summary>
/// 應用協調器 - 管理服務生命週期和事件協調
/// 整合開機自動啟動與配置同步
/// </summary>
public sealed class AppCoordinator : IHostedService, IDisposable
{
    private readonly IOrientationDetector _orientationDetector;
    private readonly WallpaperService _wallpaperService;
    private readonly IConfigurationStore _configStore;
    private readonly IEventBus _eventBus;
    private readonly IAutoStartManager _autoStartManager;
    private readonly ILogger<AppCoordinator> _logger;

    private bool _isEnabled = true;
    private ScreenOrientation _lastOrientation = ScreenOrientation.Unknown;
    private bool _disposed;

    public bool IsEnabled => _isEnabled;
    public ScreenOrientation CurrentOrientation => _lastOrientation;

    public AppCoordinator(
        IOrientationDetector orientationDetector,
        WallpaperService wallpaperService,
        IConfigurationStore configStore,
        IEventBus eventBus,
        IAutoStartManager autoStartManager,
        ILogger<AppCoordinator> logger)
    {
        _orientationDetector = orientationDetector;
        _wallpaperService = wallpaperService;
        _configStore = configStore;
        _eventBus = eventBus;
        _autoStartManager = autoStartManager;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AppCoordinator starting...");

        try
        {
            // 載入配置
            var config = await _configStore.LoadAsync();
            _isEnabled = config.Settings.Enabled;

            // 同步開機自動啟動設定
            SyncAutoStartWithConfiguration(config.Settings.StartWithWindows);

            // 訂閱配置變更事件
            _configStore.ConfigurationChanged += OnConfigurationChanged;

            // 訂閱方向變更事件
            _orientationDetector.OrientationChanged += OnOrientationChanged;

            // 開始監控
            _orientationDetector.StartMonitoring();

            // 取得當前方向
            _lastOrientation = _orientationDetector.GetCurrentOrientation();

            // 初始套用當前方向對應的桌布
            if (_isEnabled)
            {
                await SafeExecuteAsync(
                    () => _wallpaperService.SwitchForOrientationAsync(_lastOrientation),
                    "initial wallpaper switch");
            }

            _logger.LogInformation(
                "AppCoordinator started. Enabled: {Enabled}, Orientation: {Orientation}",
                _isEnabled,
                _lastOrientation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AppCoordinator startup");
            // 繼續運行，部分功能可能受影響
        }
    }

    /// <summary>
    /// 配置變更處理
    /// </summary>
    private void OnConfigurationChanged(object? sender, Core.Entities.AppConfiguration config)
    {
        try
        {
            _logger.LogInformation("Configuration changed, updating settings...");

            // 更新啟用狀態
            var previousEnabled = _isEnabled;
            _isEnabled = config.Settings.Enabled;

            if (previousEnabled != _isEnabled)
            {
                _eventBus.Publish(new ServiceStateChangedEvent(_isEnabled));
            }

            // 同步開機自動啟動設定
            SyncAutoStartWithConfiguration(config.Settings.StartWithWindows);

            // 如果啟用狀態變更為開啟，立即套用當前方向的桌布
            if (_isEnabled && !previousEnabled)
            {
                _ = SafeExecuteAsync(
                    () => _wallpaperService.SwitchForOrientationAsync(_lastOrientation),
                    "wallpaper switch on enable");
            }

            // 通知服務重新載入配置
            _ = _wallpaperService.ReloadConfigurationAsync();

            _logger.LogDebug("Configuration update completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling configuration change");
        }
    }

    /// <summary>
    /// 同步開機自動啟動設定與配置
    /// </summary>
    private void SyncAutoStartWithConfiguration(bool shouldBeEnabled)
    {
        try
        {
            var result = _autoStartManager.SyncWithConfiguration(shouldBeEnabled);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "Failed to sync auto-start setting: {Error}. Current state: {State}",
                    result.ErrorMessage,
                    result.CurrentState);
            }
            else
            {
                _logger.LogDebug("Auto-start synchronized: {State}", result.CurrentState);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing auto-start configuration");
        }
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
            "Orientation changed: {Previous} -> {Current}",
            e.PreviousOrientation,
            e.CurrentOrientation);

        // 發布領域事件
        _eventBus.Publish(new OrientationChangedEvent(e.PreviousOrientation, e.CurrentOrientation));

        await SafeExecuteAsync(
            () => _wallpaperService.SwitchForOrientationAsync(e.CurrentOrientation),
            "wallpaper switch on orientation change");
    }

    /// <summary>
    /// 安全執行異步操作，包含錯誤處理和重試邏輯
    /// </summary>
    private async Task SafeExecuteAsync(Func<Task> action, string operationName, int maxRetries = 2)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await action();
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed {Operation} (attempt {Attempt}/{MaxRetries})",
                    operationName, attempt, maxRetries);

                if (attempt < maxRetries)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt));
                }
                else
                {
                    _logger.LogError(ex, "All retry attempts failed for {Operation}", operationName);
                }
            }
        }
    }

    /// <summary>
    /// 啟用或停用服務
    /// </summary>
    public async Task SetEnabledAsync(bool enabled)
    {
        if (_isEnabled == enabled) return;

        _isEnabled = enabled;
        _logger.LogInformation("Service {Status}", enabled ? "enabled" : "disabled");

        _eventBus.Publish(new ServiceStateChangedEvent(enabled));

        // 更新配置
        try
        {
            var config = await _configStore.LoadAsync();
            config.Settings.Enabled = enabled;
            await _configStore.SaveAsync(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save enabled state to configuration");
        }

        // 如果啟用，立即套用當前方向的桌布
        if (enabled)
        {
            await SafeExecuteAsync(
                () => _wallpaperService.SwitchForOrientationAsync(_lastOrientation),
                "wallpaper switch on enable");
        }
    }

    /// <summary>
    /// 切換啟用狀態
    /// </summary>
    public Task ToggleEnabledAsync() => SetEnabledAsync(!_isEnabled);

    /// <summary>
    /// 手動切換到指定方向
    /// </summary>
    public Task SwitchToOrientationAsync(ScreenOrientation orientation) =>
        _wallpaperService.SwitchToAsync(orientation);

    /// <summary>
    /// 獲取當前自動啟動狀態
    /// </summary>
    public bool IsAutoStartEnabled() => _autoStartManager.IsAutoStartEnabled();

    /// <summary>
    /// 設定開機自動啟動
    /// </summary>
    public async Task<bool> SetAutoStartAsync(bool enabled)
    {
        var result = enabled
            ? _autoStartManager.EnableAutoStart()
            : _autoStartManager.DisableAutoStart();

        if (result.Success)
        {
            // 同步更新配置
            try
            {
                var config = await _configStore.LoadAsync();
                config.Settings.StartWithWindows = enabled;
                await _configStore.SaveAsync(config);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save auto-start state to configuration");
            }
        }

        return result.Success;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AppCoordinator stopping...");

        try
        {
            _configStore.ConfigurationChanged -= OnConfigurationChanged;
            _orientationDetector.StopMonitoring();
            _orientationDetector.OrientationChanged -= OnOrientationChanged;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AppCoordinator shutdown");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _orientationDetector.Dispose();
            _wallpaperService.Dispose();
            _autoStartManager.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during AppCoordinator disposal");
        }

        _disposed = true;
    }
}
