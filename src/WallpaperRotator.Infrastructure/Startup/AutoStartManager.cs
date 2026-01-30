using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace WallpaperRotator.Infrastructure.Startup;

/// <summary>
/// 開機自動啟動管理器
/// </summary>
public sealed class AutoStartManager
{
    private readonly ILogger<AutoStartManager> _logger;
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "WallpaperRotator";

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
            return value != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check auto-start status");
            return false;
        }
    }

    /// <summary>
    /// 啟用開機自動啟動
    /// </summary>
    public bool EnableAutoStart(string? executablePath = null)
    {
        try
        {
            executablePath ??= Environment.ProcessPath;
            if (string.IsNullOrEmpty(executablePath))
            {
                _logger.LogError("Could not determine executable path");
                return false;
            }

            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null)
            {
                _logger.LogError("Could not open registry key for writing");
                return false;
            }

            // 設定為最小化啟動
            var command = $"\"{executablePath}\" --minimized";
            key.SetValue(AppName, command, RegistryValueKind.String);

            _logger.LogInformation("Auto-start enabled: {Path}", command);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable auto-start");
            return false;
        }
    }

    /// <summary>
    /// 停用開機自動啟動
    /// </summary>
    public bool DisableAutoStart()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null)
            {
                return true; // 無法開啟意味著可能已經沒有設定
            }

            if (key.GetValue(AppName) != null)
            {
                key.DeleteValue(AppName);
                _logger.LogInformation("Auto-start disabled");
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable auto-start");
            return false;
        }
    }

    /// <summary>
    /// 切換開機自動啟動狀態
    /// </summary>
    public bool ToggleAutoStart()
    {
        if (IsAutoStartEnabled())
        {
            return DisableAutoStart();
        }
        else
        {
            return EnableAutoStart();
        }
    }
}
