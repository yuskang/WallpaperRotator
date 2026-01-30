using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using WallpaperRotator.Core.Enums;
using WallpaperRotator.Core.Interfaces;

namespace WallpaperRotator.Infrastructure.Windows;

/// <summary>
/// 桌布套用器 - 使用 Windows API 設定桌布
/// </summary>
public sealed class WallpaperApplier : IWallpaperApplier
{
    private readonly ILogger<WallpaperApplier> _logger;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };

    public WallpaperApplier(ILogger<WallpaperApplier> logger)
    {
        _logger = logger;
    }

    public async Task<WallpaperApplyResult> ApplyAsync(
        string imagePath,
        DisplayMode displayMode = DisplayMode.Fit,
        string backgroundColor = "#000000")
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 驗證圖片
            if (!ValidateImage(imagePath, out var errorMessage))
            {
                return WallpaperApplyResult.Failed(errorMessage!);
            }

            // 設定顯示模式
            await Task.Run(() => SetWallpaperStyle(displayMode));

            // 設定背景顏色 (用於 Fit 模式的留白區域)
            await Task.Run(() => SetBackgroundColor(backgroundColor));

            // 套用桌布
            var result = await Task.Run(() =>
            {
                bool success = NativeMethods.SystemParametersInfo(
                    NativeMethods.SPI_SETDESKWALLPAPER,
                    0,
                    imagePath,
                    NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDCHANGE);

                return success;
            });

            stopwatch.Stop();

            if (result)
            {
                _logger.LogDebug("Wallpaper applied: {Path}, Mode: {Mode}, Duration: {Duration}ms",
                    imagePath, displayMode, stopwatch.ElapsedMilliseconds);

                return WallpaperApplyResult.Succeeded(stopwatch.Elapsed);
            }

            var error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            return WallpaperApplyResult.Failed($"SystemParametersInfo failed with error code: {error}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply wallpaper: {Path}", imagePath);
            return WallpaperApplyResult.Failed(ex.Message);
        }
    }

    public string? GetCurrentWallpaperPath()
    {
        try
        {
            char[] buffer = new char[260];
            bool success = NativeMethods.SystemParametersInfoGet(
                NativeMethods.SPI_GETDESKWALLPAPER,
                buffer.Length,
                buffer,
                0);

            if (success)
            {
                return new string(buffer).TrimEnd('\0');
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current wallpaper path");
            return null;
        }
    }

    public bool ValidateImage(string imagePath, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(imagePath))
        {
            errorMessage = "Image path is empty";
            return false;
        }

        if (!Path.IsPathFullyQualified(imagePath))
        {
            errorMessage = "Image path must be an absolute path";
            return false;
        }

        if (!File.Exists(imagePath))
        {
            errorMessage = $"Image file not found: {imagePath}";
            return false;
        }

        var ext = Path.GetExtension(imagePath).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            errorMessage = $"Unsupported image format: {ext}. Supported formats: {string.Join(", ", AllowedExtensions)}";
            return false;
        }

        // 安全檢查：不允許系統目錄中的圖片
        var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        if (imagePath.StartsWith(systemRoot, StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "Images from Windows system directory are not allowed";
            return false;
        }

        return true;
    }

    private void SetWallpaperStyle(DisplayMode displayMode)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
            if (key == null) return;

            // 設定 WallpaperStyle
            string wallpaperStyle = displayMode switch
            {
                DisplayMode.Center => "0",
                DisplayMode.Tile => "0",
                DisplayMode.Stretch => "2",
                DisplayMode.Fit => "6",
                DisplayMode.Fill => "10",
                DisplayMode.Span => "22",
                _ => "6"
            };

            // 設定 TileWallpaper
            string tileWallpaper = displayMode == DisplayMode.Tile ? "1" : "0";

            key.SetValue("WallpaperStyle", wallpaperStyle, RegistryValueKind.String);
            key.SetValue("TileWallpaper", tileWallpaper, RegistryValueKind.String);

            _logger.LogDebug("Wallpaper style set: WallpaperStyle={Style}, TileWallpaper={Tile}",
                wallpaperStyle, tileWallpaper);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set wallpaper style");
        }
    }

    private void SetBackgroundColor(string hexColor)
    {
        try
        {
            // 解析 hex 顏色
            if (!hexColor.StartsWith('#') || hexColor.Length != 7)
            {
                hexColor = "#000000";
            }

            int r = Convert.ToInt32(hexColor.Substring(1, 2), 16);
            int g = Convert.ToInt32(hexColor.Substring(3, 2), 16);
            int b = Convert.ToInt32(hexColor.Substring(5, 2), 16);

            using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Colors", true);
            if (key == null) return;

            key.SetValue("Background", $"{r} {g} {b}", RegistryValueKind.String);

            _logger.LogDebug("Background color set: {Color}", hexColor);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set background color");
        }
    }
}
