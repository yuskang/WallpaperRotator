using WallpaperRotator.Core.Enums;

namespace WallpaperRotator.Core.Entities;

/// <summary>
/// 應用程式配置
/// </summary>
public sealed class AppConfiguration
{
    public string Version { get; set; } = "2.0";
    public GeneralSettings Settings { get; set; } = new();
    public WallpaperSettings Wallpapers { get; set; } = new();
    public ScheduleSettings Schedule { get; set; } = new();

    public static AppConfiguration CreateDefault() => new();
}

/// <summary>
/// 一般設定
/// </summary>
public sealed class GeneralSettings
{
    public bool Enabled { get; set; } = true;
    public bool StartWithWindows { get; set; } = true;
    public bool MinimizeToTray { get; set; } = true;
    public DisplayMode DisplayMode { get; set; } = DisplayMode.Fit;
    public string BackgroundColor { get; set; } = "#000000";
    public int TransitionDurationMs { get; set; } = 200;
    public string Language { get; set; } = "auto";
}

/// <summary>
/// 桌布設定
/// </summary>
public sealed class WallpaperSettings
{
    public OrientationWallpapers Landscape { get; set; } = new();
    public OrientationWallpapers Portrait { get; set; } = new();

    public OrientationWallpapers GetForOrientation(ScreenOrientation orientation) =>
        orientation == ScreenOrientation.Landscape ? Landscape : Portrait;
}

/// <summary>
/// 特定方向的桌布設定
/// </summary>
public sealed class OrientationWallpapers
{
    public RotationMode Mode { get; set; } = RotationMode.Single;
    public List<string> Images { get; set; } = new();
    public int RotateIntervalSeconds { get; set; } = 0;
    public int CurrentIndex { get; set; } = 0;

    /// <summary>
    /// 取得當前應使用的圖片路徑
    /// </summary>
    public string? GetCurrentImage()
    {
        if (Images.Count == 0) return null;
        if (CurrentIndex >= Images.Count) CurrentIndex = 0;
        return Images[CurrentIndex];
    }

    /// <summary>
    /// 移動到下一張圖片
    /// </summary>
    public void MoveToNext(Random? random = null)
    {
        if (Images.Count <= 1) return;

        if (Mode == RotationMode.Random && random != null)
        {
            CurrentIndex = random.Next(Images.Count);
        }
        else
        {
            CurrentIndex = (CurrentIndex + 1) % Images.Count;
        }
    }
}

/// <summary>
/// 排程設定
/// </summary>
public sealed class ScheduleSettings
{
    public bool Enabled { get; set; } = false;
    public List<ScheduleRule> Rules { get; set; } = new();
}

/// <summary>
/// 排程規則
/// </summary>
public sealed class ScheduleRule
{
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public WallpaperSettings Wallpapers { get; set; } = new();
}
