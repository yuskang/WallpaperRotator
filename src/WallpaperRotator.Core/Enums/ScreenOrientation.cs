namespace WallpaperRotator.Core.Enums;

/// <summary>
/// 螢幕方向
/// </summary>
public enum ScreenOrientation
{
    /// <summary>
    /// 未知方向
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 橫向 (寬 > 高)
    /// </summary>
    Landscape = 1,

    /// <summary>
    /// 直向 (高 > 寬)
    /// </summary>
    Portrait = 2
}
