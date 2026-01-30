using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WallpaperRotator.Core.Entities;
using WallpaperRotator.Core.Enums;
using WallpaperRotator.Core.Interfaces;

namespace WallpaperRotator.Presentation.ViewModels;

/// <summary>
/// 設定視窗 ViewModel
/// </summary>
public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationStore _configStore;
    private AppConfiguration _originalConfig = null!;

    private bool _hasUnsavedChanges;
    private bool _isLoading;

    // General Settings
    private bool _enabled;
    private bool _startWithWindows;
    private DisplayMode _displayMode;
    private string _backgroundColor = "#000000";
    private int _transitionDurationMs;

    // Wallpaper Settings
    private string? _landscapeImagePath;
    private string? _portraitImagePath;

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set
        {
            if (SetProperty(ref _hasUnsavedChanges, value))
            {
                SaveCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool Enabled
    {
        get => _enabled;
        set { if (SetProperty(ref _enabled, value)) MarkChanged(); }
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set { if (SetProperty(ref _startWithWindows, value)) MarkChanged(); }
    }

    public DisplayMode DisplayMode
    {
        get => _displayMode;
        set { if (SetProperty(ref _displayMode, value)) MarkChanged(); }
    }

    public string BackgroundColor
    {
        get => _backgroundColor;
        set { if (SetProperty(ref _backgroundColor, value)) MarkChanged(); }
    }

    public int TransitionDurationMs
    {
        get => _transitionDurationMs;
        set { if (SetProperty(ref _transitionDurationMs, value)) MarkChanged(); }
    }

    public string? LandscapeImagePath
    {
        get => _landscapeImagePath;
        set { if (SetProperty(ref _landscapeImagePath, value)) MarkChanged(); }
    }

    public string? PortraitImagePath
    {
        get => _portraitImagePath;
        set { if (SetProperty(ref _portraitImagePath, value)) MarkChanged(); }
    }

    public IEnumerable<DisplayMode> DisplayModes => Enum.GetValues<DisplayMode>();

    // Commands
    public AsyncRelayCommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand BrowseLandscapeCommand { get; }
    public ICommand BrowsePortraitCommand { get; }

    // Events
    public event EventHandler? SaveCompleted;
    public event EventHandler? CancelRequested;
    public event EventHandler<string>? BrowseRequested;

    public SettingsViewModel(IConfigurationStore configStore)
    {
        _configStore = configStore;

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => HasUnsavedChanges);
        CancelCommand = new RelayCommand(Cancel);
        ResetCommand = new AsyncRelayCommand(ResetAsync);
        BrowseLandscapeCommand = new RelayCommand(() => BrowseRequested?.Invoke(this, "Landscape"));
        BrowsePortraitCommand = new RelayCommand(() => BrowseRequested?.Invoke(this, "Portrait"));
    }

    public async Task LoadAsync()
    {
        IsLoading = true;

        try
        {
            _originalConfig = await _configStore.LoadAsync();
            ApplyConfiguration(_originalConfig);
            HasUnsavedChanges = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyConfiguration(AppConfiguration config)
    {
        _enabled = config.Settings.Enabled;
        _startWithWindows = config.Settings.StartWithWindows;
        _displayMode = config.Settings.DisplayMode;
        _backgroundColor = config.Settings.BackgroundColor;
        _transitionDurationMs = config.Settings.TransitionDurationMs;

        _landscapeImagePath = config.Wallpapers.Landscape.Images.FirstOrDefault();
        _portraitImagePath = config.Wallpapers.Portrait.Images.FirstOrDefault();

        // Notify all properties changed
        OnPropertyChanged(string.Empty);
    }

    private async Task SaveAsync()
    {
        var config = BuildConfiguration();
        await _configStore.SaveAsync(config);
        _originalConfig = config;
        HasUnsavedChanges = false;
        SaveCompleted?.Invoke(this, EventArgs.Empty);
    }

    private void Cancel()
    {
        ApplyConfiguration(_originalConfig);
        HasUnsavedChanges = false;
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task ResetAsync()
    {
        await _configStore.ResetToDefaultAsync();
        await LoadAsync();
    }

    private AppConfiguration BuildConfiguration()
    {
        return new AppConfiguration
        {
            Settings = new GeneralSettings
            {
                Enabled = Enabled,
                StartWithWindows = StartWithWindows,
                DisplayMode = DisplayMode,
                BackgroundColor = BackgroundColor,
                TransitionDurationMs = TransitionDurationMs
            },
            Wallpapers = new WallpaperSettings
            {
                Landscape = new OrientationWallpapers
                {
                    Images = string.IsNullOrEmpty(LandscapeImagePath)
                        ? new List<string>()
                        : new List<string> { LandscapeImagePath }
                },
                Portrait = new OrientationWallpapers
                {
                    Images = string.IsNullOrEmpty(PortraitImagePath)
                        ? new List<string>()
                        : new List<string> { PortraitImagePath }
                }
            }
        };
    }

    private void MarkChanged()
    {
        HasUnsavedChanges = true;
    }
}
