using WallpaperRotator.Core.Enums;

namespace WallpaperRotator.Core.Events;

/// <summary>
/// 領域事件基底類別
/// </summary>
public abstract class DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

/// <summary>
/// 螢幕方向變更事件
/// </summary>
public sealed class OrientationChangedEvent : DomainEvent
{
    public ScreenOrientation PreviousOrientation { get; }
    public ScreenOrientation CurrentOrientation { get; }

    public OrientationChangedEvent(
        ScreenOrientation previousOrientation,
        ScreenOrientation currentOrientation)
    {
        PreviousOrientation = previousOrientation;
        CurrentOrientation = currentOrientation;
    }
}

/// <summary>
/// 桌布已套用事件
/// </summary>
public sealed class WallpaperAppliedEvent : DomainEvent
{
    public string ImagePath { get; }
    public ScreenOrientation Orientation { get; }
    public TimeSpan ApplyDuration { get; }

    public WallpaperAppliedEvent(
        string imagePath,
        ScreenOrientation orientation,
        TimeSpan applyDuration)
    {
        ImagePath = imagePath;
        Orientation = orientation;
        ApplyDuration = applyDuration;
    }
}

/// <summary>
/// 服務狀態變更事件
/// </summary>
public sealed class ServiceStateChangedEvent : DomainEvent
{
    public bool IsEnabled { get; }

    public ServiceStateChangedEvent(bool isEnabled)
    {
        IsEnabled = isEnabled;
    }
}

/// <summary>
/// 配置變更事件
/// </summary>
public sealed class ConfigurationChangedEvent : DomainEvent
{
    public string ChangedSection { get; }

    public ConfigurationChangedEvent(string changedSection)
    {
        ChangedSection = changedSection;
    }
}
