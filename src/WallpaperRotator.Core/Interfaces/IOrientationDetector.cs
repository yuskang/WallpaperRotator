using WallpaperRotator.Core.Enums;

namespace WallpaperRotator.Core.Interfaces;

/// <summary>
/// 螢幕方向偵測器介面
/// </summary>
public interface IOrientationDetector : IDisposable
{
    /// <summary>
    /// 取得當前螢幕方向
    /// </summary>
    ScreenOrientation GetCurrentOrientation();

    /// <summary>
    /// 開始監聽方向變化
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// 停止監聽
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// 監聽狀態
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// 方向變化事件
    /// </summary>
    event EventHandler<OrientationChangedEventArgs>? OrientationChanged;
}

/// <summary>
/// 方向變化事件參數
/// </summary>
public sealed class OrientationChangedEventArgs : EventArgs
{
    public ScreenOrientation PreviousOrientation { get; }
    public ScreenOrientation CurrentOrientation { get; }
    public DateTime Timestamp { get; }

    public OrientationChangedEventArgs(
        ScreenOrientation previousOrientation,
        ScreenOrientation currentOrientation,
        DateTime timestamp)
    {
        PreviousOrientation = previousOrientation;
        CurrentOrientation = currentOrientation;
        Timestamp = timestamp;
    }
}
