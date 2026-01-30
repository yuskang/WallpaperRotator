using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WallpaperRotator.Core.Interfaces;

namespace WallpaperRotator.Infrastructure.Startup;

/// <summary>
/// 開機自動啟動管理器 - 支援配置同步和錯誤恢復
/// </summary>
public sealed class AutoStartManager : IAutoStartManager
{
    private readonly ILogger<AutoStartManager> _logger;
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "WallpaperRotator";
    private const string TaskSchedulerFallbackName = "WallpaperRotatorStartup";

    private bool _disposed;

    /// <summary>
    /// 自動啟動狀態變更事件
    /// </summary>
    public event EventHandler<bool>? AutoStartStateChanged;

    public AutoStartManager(ILogger<AutoStartManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 檢查是否已設定開機自動啟動
    /// </summary>
    public bool IsAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            var value = key?.GetValue(AppName);

            if (value == null)
            {
                return false;
            }

            // 驗證路徑是否有效
            var registeredPath = value.ToString();
            if (string.IsNullOrEmpty(registeredPath))
            {
                return false;
            }

            // 提取實際執行檔路徑（去除引號和參數）
            var execPath = ExtractExecutablePath(registeredPath);
            if (!File.Exists(execPath))
            {
                _logger.LogWarning("Auto-start is enabled but executable not found: {Path}", execPath);
                // 路徑無效但仍返回 true，讓使用者決定是否重新設定
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check auto-start status");
            return false;
        }
    }

    /// <summary>
    /// 從註冊表值中提取執行檔路徑
    /// </summary>
    private static string ExtractExecutablePath(string registryValue)
    {
        if (registryValue.StartsWith("\""))
        {
            var endQuote = registryValue.IndexOf('"', 1);
            if (endQuote > 1)
            {
                return registryValue.Substring(1, endQuote - 1);
            }
        }

        var spaceIndex = registryValue.IndexOf(' ');
        return spaceIndex > 0 ? registryValue.Substring(0, spaceIndex) : registryValue;
    }

    /// <summary>
    /// 啟用開機自動啟動
    /// </summary>
    public AutoStartResult EnableAutoStart(string? executablePath = null)
    {
        try
        {
            executablePath ??= Environment.ProcessPath;
            if (string.IsNullOrEmpty(executablePath))
            {
                var error = "Could not determine executable path";
                _logger.LogError(error);
                return AutoStartResult.Failed(error);
            }

            // 驗證執行檔存在
            if (!File.Exists(executablePath))
            {
                var error = $"Executable not found: {executablePath}";
                _logger.LogError(error);
                return AutoStartResult.Failed(error);
            }

            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null)
            {
                var error = "Could not open registry key for writing. Please run as administrator.";
                _logger.LogError(error);
                return AutoStartResult.Failed(error);
            }

            // 設定為最小化啟動
            var command = $"\"{executablePath}\" --minimized";
            key.SetValue(AppName, command, RegistryValueKind.String);

            _logger.LogInformation("Auto-start enabled: {Path}", command);

            AutoStartStateChanged?.Invoke(this, true);
            return AutoStartResult.Succeeded(true);
        }
        catch (UnauthorizedAccessException ex)
        {
            var error = "Access denied. Please run as administrator to modify startup settings.";
            _logger.LogError(ex, error);
            return AutoStartResult.Failed(error);
        }
        catch (Exception ex)
        {
            var error = $"Failed to enable auto-start: {ex.Message}";
            _logger.LogError(ex, "Failed to enable auto-start");
            return AutoStartResult.Failed(error);
        }
    }

    /// <summary>
    /// 停用開機自動啟動
    /// </summary>
    public AutoStartResult DisableAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null)
            {
                // 無法開啟意味著可能已經沒有設定
                return AutoStartResult.Succeeded(false);
            }

            if (key.GetValue(AppName) != null)
            {
                key.DeleteValue(AppName);
                _logger.LogInformation("Auto-start disabled");
            }

            AutoStartStateChanged?.Invoke(this, false);
            return AutoStartResult.Succeeded(false);
        }
        catch (UnauthorizedAccessException ex)
        {
            var error = "Access denied. Please run as administrator to modify startup settings.";
            _logger.LogError(ex, error);
            return AutoStartResult.Failed(error, IsAutoStartEnabled());
        }
        catch (Exception ex)
        {
            var error = $"Failed to disable auto-start: {ex.Message}";
            _logger.LogError(ex, "Failed to disable auto-start");
            return AutoStartResult.Failed(error, IsAutoStartEnabled());
        }
    }

    /// <summary>
    /// 切換開機自動啟動狀態
    /// </summary>
    public AutoStartResult ToggleAutoStart()
    {
        return IsAutoStartEnabled() ? DisableAutoStart() : EnableAutoStart();
    }

    /// <summary>
    /// 同步配置設定與實際註冊表狀態
    /// </summary>
    /// <param name="shouldBeEnabled">配置中設定的狀態</param>
    /// <returns>同步結果</returns>
    public AutoStartResult SyncWithConfiguration(bool shouldBeEnabled)
    {
        var currentState = IsAutoStartEnabled();

        if (currentState == shouldBeEnabled)
        {
            _logger.LogDebug("Auto-start state already synchronized: {State}", shouldBeEnabled);
            return AutoStartResult.Succeeded(currentState);
        }

        _logger.LogInformation("Synchronizing auto-start state: {Current} -> {Target}",
            currentState, shouldBeEnabled);

        return shouldBeEnabled ? EnableAutoStart() : DisableAutoStart();
    }

    /// <summary>
    /// 修復自動啟動設定（重新設定為當前執行檔路徑）
    /// </summary>
    public AutoStartResult RepairAutoStart()
    {
        try
        {
            var wasEnabled = IsAutoStartEnabled();

            if (wasEnabled)
            {
                // 先停用再重新啟用，確保路徑是最新的
                var disableResult = DisableAutoStart();
                if (!disableResult.Success)
                {
                    return disableResult;
                }
            }

            var enableResult = EnableAutoStart();
            if (enableResult.Success)
            {
                _logger.LogInformation("Auto-start repaired successfully");
            }

            return enableResult;
        }
        catch (Exception ex)
        {
            var error = $"Failed to repair auto-start: {ex.Message}";
            _logger.LogError(ex, "Failed to repair auto-start");
            return AutoStartResult.Failed(error);
        }
    }

    /// <summary>
    /// 獲取當前註冊的啟動命令
    /// </summary>
    public string? GetRegisteredCommand()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            return key?.GetValue(AppName)?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get registered command");
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
