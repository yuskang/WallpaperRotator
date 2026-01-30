using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using WallpaperRotator.Presentation.ViewModels;

namespace WallpaperRotator.Views;

/// <summary>
/// 設定視窗
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // 訂閱事件
        viewModel.SaveCompleted += (_, _) => Close();
        viewModel.CancelRequested += (_, _) => Close();
        viewModel.BrowseRequested += OnBrowseRequested;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        // 載入配置
        Loaded += async (_, _) =>
        {
            await viewModel.LoadAsync();
            UpdatePreviews();
        };
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.LandscapeImagePath) ||
            e.PropertyName == nameof(SettingsViewModel.PortraitImagePath))
        {
            UpdatePreviews();
        }
    }

    private void OnBrowseRequested(object? sender, string orientation)
    {
        var dialog = new OpenFileDialog
        {
            Title = $"選擇{(orientation == "Landscape" ? "橫向" : "直向")}桌布",
            Filter = "圖片檔案|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有檔案|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog() == true)
        {
            if (orientation == "Landscape")
            {
                _viewModel.LandscapeImagePath = dialog.FileName;
            }
            else
            {
                _viewModel.PortraitImagePath = dialog.FileName;
            }
        }
    }

    private void UpdatePreviews()
    {
        UpdatePreview(LandscapePreview, _viewModel.LandscapeImagePath);
        UpdatePreview(PortraitPreview, _viewModel.PortraitImagePath);
    }

    private static void UpdatePreview(System.Windows.Controls.Image imageControl, string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !System.IO.File.Exists(imagePath))
        {
            imageControl.Source = null;
            return;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath);
            bitmap.DecodePixelWidth = 200; // 縮圖以節省記憶體
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            imageControl.Source = bitmap;
        }
        catch
        {
            imageControl.Source = null;
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_viewModel.HasUnsavedChanges)
        {
            var result = MessageBox.Show(
                "您有未儲存的變更。確定要關閉嗎？",
                "確認",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }

        base.OnClosing(e);
    }
}
