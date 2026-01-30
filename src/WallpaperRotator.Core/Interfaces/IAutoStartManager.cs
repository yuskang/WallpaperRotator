namespace WallpaperRotator.Core.Interfaces;

/// <summary>
/// 自動啟動操作結果
/// </summary>
public sealed class AutoStartResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public bool CurrentState { get; init; }

    public static AutoStartResult Succeeded(bool currentState) => new()
    {
        Success = true,
        CurrentState = currentState
    };

    public static AutoStartResult Failed(string errorMessage, bool currentState = false) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        CurrentState = currentState
    };
}

/// <summary>
/// 開機自動啟動管理器介面
/// </summary>
public interface IAutoStartManager : IDisposable
{
    /// <summary>
    /// 自動啟動狀態變更事件
    /// </summary>
    event EventHandler<bool>? AutoStartStateChanged;

    /// <summary>
    /// 檢查是否已設定開機自動啟動
    /// </summary>
    bool IsAutoStartEnabled();

    /// <summary>
    /// 啟用開機自動啟動
    /// </summary>
    AutoStartResult EnableAutoStart(string? executablePath = null);

    /// <summary>
    /// 停用開機自動啟動
    /// </summary>
    AutoStartResult DisableAutoStart();

    /// <summary>
    /// 切換開機自動啟動狀態
    /// </summary>
    AutoStartResult ToggleAutoStart();

    /// <summary>
    /// 同步配置設定與實際註冊表狀態
    /// </summary>
    AutoStartResult SyncWithConfiguration(bool shouldBeEnabled);

    /// <summary>
    /// 修復自動啟動設定
    /// </summary>
    AutoStartResult RepairAutoStart();

    /// <summary>
    /// 獲取當前註冊的啟動命令
    /// </summary>
    string? GetRegisteredCommand();
}
