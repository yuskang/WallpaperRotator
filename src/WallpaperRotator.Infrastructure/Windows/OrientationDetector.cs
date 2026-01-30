using System.Management;
using Microsoft.Extensions.Logging;
using WallpaperRotator.Core.Enums;
using WallpaperRotator.Core.Interfaces;

namespace WallpaperRotator.Infrastructure.Windows;

/// <summary>
/// 螢幕方向偵測器 - 使用 WMI 事件訂閱實現事件驅動偵測
/// </summary>
public sealed class OrientationDetector : IOrientationDetector
{
    private readonly ILogger<OrientationDetector> _logger;
    private ManagementEventWatcher? _watcher;
    private ScreenOrientation _lastOrientation;
    private bool _isMonitoring;
    private bool _disposed;

    // WMI 查詢 - 監控顯示器配置變更
    private const string WmiQuery =
        "SELECT * FROM __InstanceModificationEvent " +
        "WITHIN 1 " +
        "WHERE TargetInstance ISA 'Win32_DesktopMonitor'";

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
            _watcher = new ManagementEventWatcher(new WqlEventQuery(WmiQuery));
            _watcher.EventArrived += OnWmiEventArrived;
            _watcher.Start();

            _isMonitoring = true;
            _logger.LogInformation("WMI orientation monitoring started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start WMI watcher, falling back to timer-based monitoring");
            StartFallbackMonitoring();
        }
    }

    private void OnWmiEventArrived(object sender, EventArrivedEventArgs e)
    {
        try
        {
            var current = GetCurrentOrientation();

            if (current != _lastOrientation && current != ScreenOrientation.Unknown)
            {
                var previous = _lastOrientation;
                _lastOrientation = current;

                _logger.LogDebug("WMI detected orientation change: {Previous} → {Current}",
                    previous, current);

                OrientationChanged?.Invoke(this, new OrientationChangedEventArgs(
                    previous, current, DateTime.UtcNow));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WMI event");
        }
    }

    private Timer? _fallbackTimer;

    private void StartFallbackMonitoring()
    {
        _fallbackTimer = new Timer(CheckOrientationCallback, null,
            TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        _isMonitoring = true;
        _logger.LogInformation("Fallback timer-based monitoring started");
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

                OrientationChanged?.Invoke(this, new OrientationChangedEventArgs(
                    previous, current, DateTime.UtcNow));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fallback orientation check");
        }
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring) return;

        try
        {
            _watcher?.Stop();
            _fallbackTimer?.Dispose();
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
        _watcher?.Dispose();
        _fallbackTimer?.Dispose();
        _disposed = true;
    }
}
