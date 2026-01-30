using WallpaperRotator.Core.Enums;

namespace WallpaperRotator.Core.Interfaces;

/// <summary>
/// 過渡效果類型
/// </summary>
public enum TransitionType
{
    /// <summary>
    /// 無過渡效果（立即切換）
    /// </summary>
    None,

    /// <summary>
    /// 淡入淡出
    /// </summary>
    Fade,

    /// <summary>
    /// 滑動
    /// </summary>
    Slide,

    /// <summary>
    /// 縮放
    /// </summary>
    Zoom,

    /// <summary>
    /// 交叉淡入淡出
    /// </summary>
    CrossFade,

    /// <summary>
    /// 模糊過渡
    /// </summary>
    Blur
}

/// <summary>
/// 過渡效果配置
/// </summary>
public sealed class TransitionOptions
{
    /// <summary>
    /// 過渡類型
    /// </summary>
    public TransitionType Type { get; init; } = TransitionType.None;

    /// <summary>
    /// 過渡持續時間（毫秒）
    /// </summary>
    public int DurationMs { get; init; } = 200;

    /// <summary>
    /// 緩動函數類型
    /// </summary>
    public EasingType Easing { get; init; } = EasingType.EaseInOut;

    /// <summary>
    /// 額外參數（例如滑動方向）
    /// </summary>
    public Dictionary<string, object>? Parameters { get; init; }

    /// <summary>
    /// 建立預設無過渡選項
    /// </summary>
    public static TransitionOptions None => new() { Type = TransitionType.None, DurationMs = 0 };

    /// <summary>
    /// 建立淡入淡出過渡選項
    /// </summary>
    public static TransitionOptions Fade(int durationMs = 300) => new()
    {
        Type = TransitionType.Fade,
        DurationMs = durationMs,
        Easing = EasingType.EaseInOut
    };
}

/// <summary>
/// 緩動函數類型
/// </summary>
public enum EasingType
{
    Linear,
    EaseIn,
    EaseOut,
    EaseInOut,
    EaseInQuad,
    EaseOutQuad,
    EaseInOutQuad
}

/// <summary>
/// 過渡效果結果
/// </summary>
public sealed class TransitionResult
{
    public bool Success { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }

    public static TransitionResult Succeeded(TimeSpan duration) => new()
    {
        Success = true,
        Duration = duration
    };

    public static TransitionResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// 桌布過渡效果介面
/// </summary>
/// <remarks>
/// 此介面預留給未來實現桌布切換動畫效果。
/// Windows 原生桌布 API 不支援過渡動畫，
/// 需要使用覆蓋視窗或其他技術實現。
/// </remarks>
public interface IWallpaperTransition
{
    /// <summary>
    /// 是否支援過渡效果
    /// </summary>
    bool IsTransitionSupported { get; }

    /// <summary>
    /// 當前支援的過渡類型
    /// </summary>
    IReadOnlyList<TransitionType> SupportedTransitions { get; }

    /// <summary>
    /// 準備過渡效果（擷取當前畫面等）
    /// </summary>
    /// <param name="options">過渡選項</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<bool> PrepareTransitionAsync(TransitionOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// 執行過渡效果
    /// </summary>
    /// <param name="fromImagePath">來源圖片路徑（可為 null 表示從當前畫面）</param>
    /// <param name="toImagePath">目標圖片路徑</param>
    /// <param name="options">過渡選項</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<TransitionResult> ExecuteTransitionAsync(
        string? fromImagePath,
        string toImagePath,
        TransitionOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取消當前過渡效果
    /// </summary>
    Task CancelTransitionAsync();

    /// <summary>
    /// 清理過渡資源
    /// </summary>
    Task CleanupAsync();
}

/// <summary>
/// 空過渡效果實現（不執行任何動畫）
/// </summary>
public sealed class NoTransition : IWallpaperTransition
{
    public bool IsTransitionSupported => false;

    public IReadOnlyList<TransitionType> SupportedTransitions => new[] { TransitionType.None };

    public Task<bool> PrepareTransitionAsync(TransitionOptions options, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<TransitionResult> ExecuteTransitionAsync(
        string? fromImagePath,
        string toImagePath,
        TransitionOptions options,
        CancellationToken cancellationToken = default)
        => Task.FromResult(TransitionResult.Succeeded(TimeSpan.Zero));

    public Task CancelTransitionAsync() => Task.CompletedTask;

    public Task CleanupAsync() => Task.CompletedTask;
}
