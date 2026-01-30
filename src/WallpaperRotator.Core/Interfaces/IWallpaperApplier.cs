using WallpaperRotator.Core.Enums;

namespace WallpaperRotator.Core.Interfaces;

/// <summary>
/// 桌布套用器介面
/// </summary>
public interface IWallpaperApplier
{
    /// <summary>
    /// 套用桌布
    /// </summary>
    /// <param name="imagePath">圖片完整路徑</param>
    /// <param name="displayMode">顯示模式</param>
    /// <param name="backgroundColor">背景顏色 (Fit 模式留白區)</param>
    /// <returns>套用結果</returns>
    Task<WallpaperApplyResult> ApplyAsync(
        string imagePath,
        DisplayMode displayMode = DisplayMode.Fit,
        string backgroundColor = "#000000");

    /// <summary>
    /// 取得當前桌布路徑
    /// </summary>
    string? GetCurrentWallpaperPath();

    /// <summary>
    /// 驗證圖片是否可用作桌布
    /// </summary>
    bool ValidateImage(string imagePath, out string? errorMessage);
}

/// <summary>
/// 桌布套用結果
/// </summary>
public sealed class WallpaperApplyResult
{
    public bool Success { get; }
    public string? ErrorMessage { get; }
    public TimeSpan ApplyDuration { get; }

    private WallpaperApplyResult(bool success, string? errorMessage, TimeSpan applyDuration)
    {
        Success = success;
        ErrorMessage = errorMessage;
        ApplyDuration = applyDuration;
    }

    public static WallpaperApplyResult Succeeded(TimeSpan duration) =>
        new(true, null, duration);

    public static WallpaperApplyResult Failed(string errorMessage) =>
        new(false, errorMessage, TimeSpan.Zero);
}
