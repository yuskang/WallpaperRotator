using System.Windows;

namespace WallpaperRotator;

/// <summary>
/// 應用程式入口點
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 檢查是否已有實例在運行
        using var mutex = new Mutex(true, "WallpaperRotator_SingleInstance", out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show(
                "WallpaperRotator 已經在運行中。\n請檢查系統托盤圖示。",
                "WallpaperRotator",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
