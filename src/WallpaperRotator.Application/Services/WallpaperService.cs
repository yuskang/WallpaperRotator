using Microsoft.Extensions.Logging;
using WallpaperRotator.Core.Entities;
using WallpaperRotator.Core.Enums;
using WallpaperRotator.Core.Events;
using WallpaperRotator.Core.Interfaces;

namespace WallpaperRotator.Application.Services;

/// <summary>
/// 桌布服務 - 管理桌布切換邏輯
/// </summary>
public sealed class WallpaperService : IDisposable
{
    private readonly IWallpaperApplier _applier;
    private readonly IConfigurationStore _configStore;
    private readonly IEventBus _eventBus;
    private readonly ILogger<WallpaperService> _logger;
    private readonly Random _random = new();

    private AppConfiguration? _currentConfig;

    public WallpaperService(
        IWallpaperApplier applier,
        IConfigurationStore configStore,
        IEventBus eventBus,
        ILogger<WallpaperService> logger)
    {
        _applier = applier;
        _configStore = configStore;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <summary>
    /// 根據螢幕方向切換桌布
    /// </summary>
    public async Task<bool> SwitchForOrientationAsync(ScreenOrientation orientation)
    {
        try
        {
            _currentConfig ??= await _configStore.LoadAsync();

            var wallpaperSettings = _currentConfig.Wallpapers.GetForOrientation(orientation);
            var imagePath = wallpaperSettings.GetCurrentImage();

            if (string.IsNullOrEmpty(imagePath))
            {
                _logger.LogWarning("No wallpaper configured for orientation: {Orientation}", orientation);
                return false;
            }

            if (!_applier.ValidateImage(imagePath, out var errorMessage))
            {
                _logger.LogError("Invalid wallpaper image: {Path}, Error: {Error}", imagePath, errorMessage);
                return false;
            }

            var result = await _applier.ApplyAsync(
                imagePath,
                _currentConfig.Settings.DisplayMode,
                _currentConfig.Settings.BackgroundColor);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Wallpaper switched to {Path} for {Orientation} in {Duration}ms",
                    imagePath,
                    orientation,
                    result.ApplyDuration.TotalMilliseconds);

                _eventBus.Publish(new WallpaperAppliedEvent(imagePath, orientation, result.ApplyDuration));
                return true;
            }

            _logger.LogError("Failed to apply wallpaper: {Error}", result.ErrorMessage);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching wallpaper for orientation: {Orientation}", orientation);
            return false;
        }
    }

    /// <summary>
    /// 手動切換到指定方向的桌布
    /// </summary>
    public Task<bool> SwitchToAsync(ScreenOrientation orientation) =>
        SwitchForOrientationAsync(orientation);

    /// <summary>
    /// 輪換到下一張桌布
    /// </summary>
    public async Task<bool> RotateNextAsync(ScreenOrientation orientation)
    {
        _currentConfig ??= await _configStore.LoadAsync();

        var wallpaperSettings = _currentConfig.Wallpapers.GetForOrientation(orientation);
        wallpaperSettings.MoveToNext(_random);

        await _configStore.SaveAsync(_currentConfig);

        return await SwitchForOrientationAsync(orientation);
    }

    /// <summary>
    /// 重新載入配置
    /// </summary>
    public async Task ReloadConfigurationAsync()
    {
        _currentConfig = await _configStore.LoadAsync();
        _logger.LogInformation("Configuration reloaded");
    }

    public void Dispose()
    {
        // 清理資源
    }
}
