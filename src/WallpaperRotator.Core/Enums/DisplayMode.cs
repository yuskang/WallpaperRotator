namespace WallpaperRotator.Core.Enums;

/// <summary>
/// 桌布顯示模式
/// </summary>
public enum DisplayMode
{
    /// <summary>
    /// 置中 (WallpaperStyle=0, TileWallpaper=0)
    /// </summary>
    Center = 0,

    /// <summary>
    /// 並排 (WallpaperStyle=0, TileWallpaper=1)
    /// </summary>
    Tile = 1,

    /// <summary>
    /// 延展 (WallpaperStyle=2, TileWallpaper=0)
    /// </summary>
    Stretch = 2,

    /// <summary>
    /// 符合 - 保持比例，可能留白 (WallpaperStyle=6, TileWallpaper=0)
    /// </summary>
    Fit = 6,

    /// <summary>
    /// 填滿 - 保持比例，可能裁切 (WallpaperStyle=10, TileWallpaper=0)
    /// </summary>
    Fill = 10,

    /// <summary>
    /// 跨螢幕 (WallpaperStyle=22, TileWallpaper=0)
    /// </summary>
    Span = 22
}
