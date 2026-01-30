using System.Management;
using Microsoft.Extensions.Logging;
using WallpaperRotator.Core.Enums;
using WallpaperRotator.Core.Interfaces;

namespace WallpaperRotator.Infrastructure.Windows;

/// <summary>
/// 螢幕方向偵測器 - 使用 Timer 輪詢實現方向偵測
/// </summary>
/// <remarks>
/// WMI Win32_DesktopMonitor 事件無法偵測螢幕旋轉，
/// 因此使用 Timer 輪詢 GetSystemMetrics API 來偵測方向變化。
/// </remarks>
public sealed class OrientationDetector : IOrientationDetector
{
    private readonly ILogger<OrientationDetector> _logger;
    private Timer? _pollingTimer;
    private ScreenOrientation _lastOrientation;
    private bool _isMonitoring;
    private bool _disposed;

    // 輪詢間隔 (毫秒)
    private const int PollingIntervalMs = 1000;

    public bool IsMonitoring => _isMonitoring;

    public event EventHandler<OrientationChangedEventArgs>? OrientationChanged;

    public OrientationDetector(ILogger<OrientationDetector> logger)
    {
        _logger = logger;
        _lastOrientation = GetCurrentOrientation();
    }

    public ScreenOrientation GetCurrentOrientation()
    {
        try
        {
            int width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
            int height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);

            var orientation = width > height
                ? ScreenOrientation.Landscape
                : ScreenOrientation.Portrait;

            _logger.LogDebug("Screen dimensions: {Width}x{Height}, Orientation: {Orientation}",
                width, height, orientation);

            return orientation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get screen orientation");
            return ScreenOrientation.Unknown;
        }
    }

    public void StartMonitoring()
    {
        if (_isMonitoring) return;

        try
        {
            _pollingTimer = new Timer(
                CheckOrientationCallback,
                null,
                TimeSpan.FromMilliseconds(PollingIntervalMs),
                TimeSpan.FromMilliseconds(PollingIntervalMs));

            _isMonitoring = true;
            _logger.LogInformation("Orientation polling started (interval: {Interval}ms)", PollingIntervalMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start orientation monitoring");
        }
    }

    private void CheckOrientationCallback(object? state)
    {
        try
        {
            var current = GetCurrentOrientation();

            if (current != _lastOrientation && current != ScreenOrientation.Unknown)
            {
                var previous = _lastOrientation;
                _lastOrientation = current;

                _logger.LogInformation("Orientation changed: {Previous} → {Current}",
                    previous, current);

                OrientationChanged?.Invoke(this, new OrientationChangedEventArgs(
                    previous, current, DateTime.UtcNow));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in orientation check");
        }
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring) return;

        try
        {
            _pollingTimer?.Dispose();
            _pollingTimer = null;
            _isMonitoring = false;
            _logger.LogInformation("Orientation monitoring stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping orientation monitoring");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        StopMonitoring();
        _disposed = true;
    }
}
