using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using WallpaperRotator.Presentation.ViewModels;

namespace WallpaperRotator.Views;

/// <summary>
/// 系統托盤圖示視窗
/// </summary>
public partial class TrayIconWindow : Window
{
    private readonly TrayIconViewModel _viewModel;

    public TrayIconWindow(TrayIconViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // 設定圖示
        SetTrayIcon();

        // 監聽狀態變化以更新圖示
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(viewModel.IsEnabled) ||
                e.PropertyName == nameof(viewModel.CurrentOrientation))
            {
                SetTrayIcon();
            }
        };
    }

    private void SetTrayIcon()
    {
        // 動態生成圖示 (根據狀態顯示不同顏色)
        var icon = CreateIcon(_viewModel.IsEnabled, _viewModel.CurrentOrientation);
        TrayIcon.Icon = icon;
    }

    private static System.Drawing.Icon CreateIcon(bool isEnabled, Core.Enums.ScreenOrientation orientation)
    {
        // 建立一個簡單的圖示
        // 實際應用中應該使用資源檔案中的圖示
        var color = isEnabled
            ? (orientation == Core.Enums.ScreenOrientation.Landscape
                ? System.Drawing.Color.DodgerBlue
                : System.Drawing.Color.ForestGreen)
            : System.Drawing.Color.Gray;

        using var bitmap = new System.Drawing.Bitmap(32, 32);
        using var graphics = System.Drawing.Graphics.FromImage(bitmap);

        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(System.Drawing.Color.Transparent);

        // 繪製圓形圖示
        using var brush = new System.Drawing.SolidBrush(color);
        graphics.FillEllipse(brush, 2, 2, 28, 28);

        // 繪製方向指示
        using var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2);
        if (orientation == Core.Enums.ScreenOrientation.Landscape)
        {
            // 橫向：繪製橫線
            graphics.DrawLine(pen, 8, 16, 24, 16);
        }
        else
        {
            // 直向：繪製直線
            graphics.DrawLine(pen, 16, 8, 16, 24);
        }

        // 轉換為 Icon
        var hIcon = bitmap.GetHicon();
        return System.Drawing.Icon.FromHandle(hIcon);
    }

    protected override void OnClosed(EventArgs e)
    {
        TrayIcon.Dispose();
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
