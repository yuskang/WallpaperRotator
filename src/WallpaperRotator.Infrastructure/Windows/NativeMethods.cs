using System.Runtime.InteropServices;

namespace WallpaperRotator.Infrastructure.Windows;

/// <summary>
/// Windows API 原生方法定義
/// </summary>
internal static partial class NativeMethods
{
    // 螢幕尺寸常數
    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;

    // 桌布設定常數
    public const int SPI_SETDESKWALLPAPER = 0x0014;
    public const int SPI_GETDESKWALLPAPER = 0x0073;
    public const int SPIF_UPDATEINIFILE = 0x01;
    public const int SPIF_SENDCHANGE = 0x02;

    // 取得螢幕尺寸
    [LibraryImport("user32.dll")]
    public static partial int GetSystemMetrics(int nIndex);

    // 設定系統參數 (設定桌布)
    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SystemParametersInfo(
        int uAction,
        int uParam,
        string? lpvParam,
        int fuWinIni);

    // 取得系統參數 (取得當前桌布路徑)
    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SystemParametersInfoGet(
        int uAction,
        int uParam,
        [Out] char[] lpvParam,
        int fuWinIni);
}
