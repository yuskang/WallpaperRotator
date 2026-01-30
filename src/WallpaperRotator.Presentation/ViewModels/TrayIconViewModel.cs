using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WallpaperRotator.Application.Coordinators;
using WallpaperRotator.Core.Enums;
using WallpaperRotator.Core.Events;
using WallpaperRotator.Core.Interfaces;

namespace WallpaperRotator.Presentation.ViewModels;

/// <summary>
/// 系統托盤圖示 ViewModel
/// </summary>
public sealed class TrayIconViewModel : ViewModelBase, IDisposable
{
    private readonly AppCoordinator _coordinator;
    private readonly IEventBus _eventBus;
    private readonly IDisposable _orientationSubscription;
    private readonly IDisposable _stateSubscription;

    private bool _isEnabled;
    private ScreenOrientation _currentOrientation;
    private string _statusText = "WallpaperRotator";
    private string _toolTipText = "WallpaperRotator - 啟動中...";

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                _coordinator.SetEnabled(value);
                UpdateStatusText();
            }
        }
    }

    public ScreenOrientation CurrentOrientation
    {
        get => _currentOrientation;
        private set
        {
            if (SetProperty(ref _currentOrientation, value))
            {
                UpdateStatusText();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public string ToolTipText
    {
        get => _toolTipText;
        private set => SetProperty(ref _toolTipText, value);
    }

    public string OrientationDisplayText => CurrentOrientation switch
    {
        ScreenOrientation.Landscape => "橫向",
        ScreenOrientation.Portrait => "直向",
        _ => "未知"
    };

    // Commands
    public ICommand ToggleEnabledCommand { get; }
    public ICommand SwitchToLandscapeCommand { get; }
    public ICommand SwitchToPortraitCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand ExitCommand { get; }

    // Events
    public event EventHandler? SettingsRequested;
    public event EventHandler? ExitRequested;

    public TrayIconViewModel(AppCoordinator coordinator, IEventBus eventBus)
    {
        _coordinator = coordinator;
        _eventBus = eventBus;

        _isEnabled = coordinator.IsEnabled;
        _currentOrientation = coordinator.CurrentOrientation;

        // Initialize commands
        ToggleEnabledCommand = new RelayCommand(ToggleEnabled);
        SwitchToLandscapeCommand = new AsyncRelayCommand(SwitchToLandscapeAsync);
        SwitchToPortraitCommand = new AsyncRelayCommand(SwitchToPortraitAsync);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        ExitCommand = new RelayCommand(Exit);

        // Subscribe to events
        _orientationSubscription = eventBus.Subscribe<OrientationChangedEvent>(OnOrientationChanged);
        _stateSubscription = eventBus.Subscribe<ServiceStateChangedEvent>(OnStateChanged);

        UpdateStatusText();
    }

    private void ToggleEnabled()
    {
        IsEnabled = !IsEnabled;
    }

    private async Task SwitchToLandscapeAsync()
    {
        await _coordinator.SwitchToOrientationAsync(ScreenOrientation.Landscape);
    }

    private async Task SwitchToPortraitAsync()
    {
        await _coordinator.SwitchToOrientationAsync(ScreenOrientation.Portrait);
    }

    private void OpenSettings()
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Exit()
    {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OnOrientationChanged(OrientationChangedEvent evt)
    {
        CurrentOrientation = evt.CurrentOrientation;
    }

    private void OnStateChanged(ServiceStateChangedEvent evt)
    {
        _isEnabled = evt.IsEnabled;
        OnPropertyChanged(nameof(IsEnabled));
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (IsEnabled)
        {
            StatusText = $"運行中 - {OrientationDisplayText}";
            ToolTipText = $"WallpaperRotator\n狀態: 運行中\n方向: {OrientationDisplayText}";
        }
        else
        {
            StatusText = "已暫停";
            ToolTipText = "WallpaperRotator\n狀態: 已暫停";
        }
    }

    public void Dispose()
    {
        _orientationSubscription.Dispose();
        _stateSubscription.Dispose();
    }
}
