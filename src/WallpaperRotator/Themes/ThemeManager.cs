using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;

namespace WallpaperRotator.Themes;

/// <summary>
/// 主題管理器 - 支援深色/淺色主題自動切換
/// </summary>
public static class ThemeManager
{
    private const string LightThemePath = "Resources/Styles.xaml";
    private const string DarkThemePath = "Resources/DarkTheme.xaml";

    private static ResourceDictionary? _currentThemeDictionary;

    /// <summary>
    /// 當前是否為深色主題
    /// </summary>
    public static bool IsDarkTheme { get; private set; }

    /// <summary>
    /// 主題變更事件
    /// </summary>
    public static event EventHandler<bool>? ThemeChanged;

    /// <summary>
    /// 初始化主題管理器
    /// </summary>
    public static void Initialize()
    {
        // 偵測系統主題並套用
        ApplySystemTheme();

        // 監聽系統主題變更
        SystemEvents.UserPreferenceChanged += OnSystemPreferenceChanged;
    }

    /// <summary>
    /// 清理資源
    /// </summary>
    public static void Cleanup()
    {
        SystemEvents.UserPreferenceChanged -= OnSystemPreferenceChanged;
    }

    /// <summary>
    /// 套用系統主題
    /// </summary>
    public static void ApplySystemTheme()
    {
        bool isDark = IsSystemDarkTheme();
        ApplyTheme(isDark);
    }

    /// <summary>
    /// 套用指定主題
    /// </summary>
    /// <param name="isDark">是否為深色主題</param>
    public static void ApplyTheme(bool isDark)
    {
        if (IsDarkTheme == isDark && _currentThemeDictionary != null)
            return;

        IsDarkTheme = isDark;

        Application.Current.Dispatcher.Invoke(() =>
        {
            var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

            // 移除當前主題
            if (_currentThemeDictionary != null)
            {
                mergedDictionaries.Remove(_currentThemeDictionary);
            }

            // 載入新主題
            var themePath = isDark ? DarkThemePath : LightThemePath;
            _currentThemeDictionary = new ResourceDictionary
            {
                Source = new Uri(themePath, UriKind.Relative)
            };

            // 確保主題在樣式之後載入
            mergedDictionaries.Add(_currentThemeDictionary);

            ThemeChanged?.Invoke(null, isDark);
        });
    }

    /// <summary>
    /// 切換主題
    /// </summary>
    public static void ToggleTheme()
    {
        ApplyTheme(!IsDarkTheme);
    }

    /// <summary>
    /// 偵測系統是否使用深色主題
    /// </summary>
    private static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            if (key != null)
            {
                var value = key.GetValue("AppsUseLightTheme");
                if (value is int intValue)
                {
                    return intValue == 0; // 0 = 深色, 1 = 淺色
                }
            }
        }
        catch
        {
            // 無法讀取註冊表，使用預設淺色主題
        }

        return false;
    }

    /// <summary>
    /// 系統偏好設定變更處理
    /// </summary>
    private static void OnSystemPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            ApplySystemTheme();
        }
    }
}
