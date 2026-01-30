# 技術架構設計文檔: WallpaperRotator v2.0

> System Architecture Design Document

| 欄位 | 內容 |
|------|------|
| **產品名稱** | WallpaperRotator |
| **版本** | v2.0 |
| **文檔版本** | 1.0 |
| **建立日期** | 2026-01-30 |
| **負責人** | Architect Agent |
| **狀態** | Draft |
| **關聯文檔** | PRD-WallpaperRotator-v2.0.md |

---

## 1. 架構概述

### 1.1 設計原則

| 原則 | 說明 | 實踐方式 |
|------|------|----------|
| **單一職責** | 每個模組只負責一項功能 | 核心引擎、UI、配置分離 |
| **事件驅動** | 響應式而非輪詢式 | WMI 事件訂閱 |
| **低資源佔用** | 最小化系統影響 | 無 Timer、按需喚醒 |
| **可擴展性** | 支援未來功能擴展 | 插件式架構、介面抽象 |
| **容錯性** | 優雅處理異常 | 全局異常捕獲、狀態恢復 |

### 1.2 系統架構總覽

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         WallpaperRotator v2.0                            │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                        表現層 (Presentation Layer)                  │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌────────────────────────┐   │ │
│  │  │ TrayIconView │  │ SettingsView │  │ SetupWizardView        │   │ │
│  │  │ 系統托盤     │  │ 設定視窗     │  │ 首次配置精靈           │   │ │
│  │  └──────┬───────┘  └──────┬───────┘  └───────────┬────────────┘   │ │
│  │         │                 │                      │                 │ │
│  │  ┌──────┴─────────────────┴──────────────────────┴──────────────┐ │ │
│  │  │                    ViewModel Layer (MVVM)                     │ │ │
│  │  │  TrayIconVM  │  SettingsVM  │  SetupWizardVM  │  MainVM      │ │ │
│  │  └──────────────────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                    │                                     │
│                                    ▼                                     │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                        應用層 (Application Layer)                   │ │
│  │  ┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐ │ │
│  │  │ OrientationSvc   │  │ WallpaperSvc     │  │ SchedulerSvc     │ │ │
│  │  │ 方向偵測服務     │  │ 桌布切換服務     │  │ 排程服務         │ │ │
│  │  └────────┬─────────┘  └────────┬─────────┘  └────────┬─────────┘ │ │
│  │           │                     │                     │           │ │
│  │  ┌────────┴─────────────────────┴─────────────────────┴─────────┐ │ │
│  │  │                      AppCoordinator                           │ │ │
│  │  │                   (應用協調器/事件總線)                        │ │ │
│  │  └──────────────────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                    │                                     │
│                                    ▼                                     │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                        領域層 (Domain Layer)                        │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐ │ │
│  │  │ Orientation  │  │ Wallpaper    │  │ Schedule                 │ │ │
│  │  │ 方向實體     │  │ 桌布實體     │  │ 排程實體                 │ │ │
│  │  └──────────────┘  └──────────────┘  └──────────────────────────┘ │ │
│  │  ┌──────────────────────────────────────────────────────────────┐ │ │
│  │  │                    Domain Events                              │ │ │
│  │  │  OrientationChanged │ WallpaperApplied │ ScheduleTriggered   │ │ │
│  │  └──────────────────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                    │                                     │
│                                    ▼                                     │
│  ┌────────────────────────────────────────────────────────────────────┐ │
│  │                      基礎設施層 (Infrastructure Layer)              │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐ │ │
│  │  │ WinApiGateway│  │ ConfigStore  │  │ FileSystemAccess         │ │ │
│  │  │ Windows API  │  │ 配置儲存     │  │ 檔案系統                 │ │ │
│  │  └──────────────┘  └──────────────┘  └──────────────────────────┘ │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────┐ │ │
│  │  │ RegistryAccess│ │ LoggingInfra │  │ AutoStartManager         │ │ │
│  │  │ 註冊表存取   │  │ 日誌基礎設施 │  │ 開機自啟管理             │ │ │
│  │  └──────────────┘  └──────────────┘  └──────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────┘ │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           外部依賴 (External)                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────────────┐  │
│  │ Windows API  │  │ Registry     │  │ File System                  │  │
│  │ user32.dll   │  │ HKCU         │  │ %AppData%                    │  │
│  └──────────────┘  └──────────────┘  └──────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 2. 模組詳細設計

### 2.1 模組清單

| 模組 | 命名空間 | 職責 | 依賴 |
|------|----------|------|------|
| **Core** | `WallpaperRotator.Core` | 領域模型、事件定義 | - |
| **Application** | `WallpaperRotator.Application` | 業務邏輯、服務協調 | Core |
| **Infrastructure** | `WallpaperRotator.Infrastructure` | 外部系統整合 | Core, Application |
| **Presentation** | `WallpaperRotator.Presentation` | UI 層 | Core, Application |
| **Host** | `WallpaperRotator` | 應用程式入口、DI 配置 | All |

### 2.2 專案結構

```
WallpaperRotator/
├── src/
│   ├── WallpaperRotator.Core/
│   │   ├── Entities/
│   │   │   ├── Orientation.cs
│   │   │   ├── Wallpaper.cs
│   │   │   ├── WallpaperCollection.cs
│   │   │   └── Schedule.cs
│   │   ├── Events/
│   │   │   ├── DomainEvent.cs
│   │   │   ├── OrientationChangedEvent.cs
│   │   │   ├── WallpaperAppliedEvent.cs
│   │   │   └── ScheduleTriggeredEvent.cs
│   │   ├── Enums/
│   │   │   ├── ScreenOrientation.cs
│   │   │   ├── DisplayMode.cs
│   │   │   └── RotationMode.cs
│   │   ├── Interfaces/
│   │   │   ├── IOrientationDetector.cs
│   │   │   ├── IWallpaperApplier.cs
│   │   │   ├── IConfigurationStore.cs
│   │   │   └── IEventBus.cs
│   │   └── WallpaperRotator.Core.csproj
│   │
│   ├── WallpaperRotator.Application/
│   │   ├── Services/
│   │   │   ├── OrientationService.cs
│   │   │   ├── WallpaperService.cs
│   │   │   ├── SchedulerService.cs
│   │   │   └── AutoStartService.cs
│   │   ├── Coordinators/
│   │   │   └── AppCoordinator.cs
│   │   ├── Configuration/
│   │   │   ├── AppConfiguration.cs
│   │   │   └── WallpaperConfiguration.cs
│   │   └── WallpaperRotator.Application.csproj
│   │
│   ├── WallpaperRotator.Infrastructure/
│   │   ├── Windows/
│   │   │   ├── WinApiGateway.cs
│   │   │   ├── OrientationDetector.cs
│   │   │   ├── WallpaperApplier.cs
│   │   │   └── NativeMethods.cs
│   │   ├── Storage/
│   │   │   ├── JsonConfigurationStore.cs
│   │   │   ├── RegistryAccess.cs
│   │   │   └── FileSystemAccess.cs
│   │   ├── Logging/
│   │   │   └── FileLogger.cs
│   │   ├── Startup/
│   │   │   └── AutoStartManager.cs
│   │   └── WallpaperRotator.Infrastructure.csproj
│   │
│   ├── WallpaperRotator.Presentation/
│   │   ├── Views/
│   │   │   ├── TrayIconView.xaml
│   │   │   ├── SettingsWindow.xaml
│   │   │   ├── SetupWizardWindow.xaml
│   │   │   └── Components/
│   │   │       ├── WallpaperPreview.xaml
│   │   │       └── ImageSelector.xaml
│   │   ├── ViewModels/
│   │   │   ├── ViewModelBase.cs
│   │   │   ├── TrayIconViewModel.cs
│   │   │   ├── SettingsViewModel.cs
│   │   │   └── SetupWizardViewModel.cs
│   │   ├── Converters/
│   │   │   └── BoolToVisibilityConverter.cs
│   │   ├── Resources/
│   │   │   ├── Styles.xaml
│   │   │   ├── Icons/
│   │   │   └── Strings/
│   │   │       ├── Strings.zh-TW.resx
│   │   │       ├── Strings.zh-CN.resx
│   │   │       └── Strings.en-US.resx
│   │   └── WallpaperRotator.Presentation.csproj
│   │
│   └── WallpaperRotator/
│       ├── App.xaml
│       ├── App.xaml.cs
│       ├── Program.cs
│       ├── ServiceConfiguration.cs
│       └── WallpaperRotator.csproj
│
├── tests/
│   ├── WallpaperRotator.Core.Tests/
│   ├── WallpaperRotator.Application.Tests/
│   └── WallpaperRotator.Infrastructure.Tests/
│
├── docs/
│   ├── PRD-WallpaperRotator-v2.0.md
│   └── ARCHITECTURE-WallpaperRotator-v2.0.md
│
├── assets/
│   ├── icons/
│   └── screenshots/
│
├── WallpaperRotator.sln
├── Directory.Build.props
└── README.md
```

---

## 3. 核心元件設計

### 3.1 螢幕方向偵測 (OrientationDetector)

#### 3.1.1 設計方案比較

| 方案 | 延遲 | CPU 佔用 | 複雜度 | 選擇 |
|------|------|----------|--------|------|
| Timer 輪詢 | 輪詢間隔 | 持續消耗 | 低 | ❌ |
| WMI 事件訂閱 | ~100ms | 按需喚醒 | 中 | ✅ 主方案 |
| DisplayConfigChanged 事件 | ~50ms | 按需喚醒 | 中 | ✅ 備選 |
| PowerSettingNotification | ~50ms | 按需喚醒 | 高 | ❌ |

#### 3.1.2 實現架構

```
┌─────────────────────────────────────────────────────────────┐
│                    OrientationDetector                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────────────┐     ┌─────────────────────────┐   │
│  │  WmiEventWatcher    │     │  DisplayChangeWatcher   │   │
│  │  (主要偵測器)        │     │  (備用偵測器)           │   │
│  └──────────┬──────────┘     └───────────┬─────────────┘   │
│             │                            │                  │
│             └──────────┬─────────────────┘                  │
│                        ▼                                     │
│  ┌─────────────────────────────────────────────────────────┐│
│  │                  OrientationResolver                     ││
│  │  - GetCurrentOrientation()                               ││
│  │  - CompareWithPrevious()                                 ││
│  │  - RaiseOrientationChangedEvent()                        ││
│  └─────────────────────────────────────────────────────────┘│
│                        │                                     │
│                        ▼                                     │
│  ┌─────────────────────────────────────────────────────────┐│
│  │              IEventBus.Publish()                         ││
│  │              → OrientationChangedEvent                   ││
│  └─────────────────────────────────────────────────────────┘│
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

#### 3.1.3 介面定義

```csharp
namespace WallpaperRotator.Core.Interfaces;

public interface IOrientationDetector : IDisposable
{
    /// <summary>
    /// 取得當前螢幕方向
    /// </summary>
    ScreenOrientation GetCurrentOrientation();

    /// <summary>
    /// 開始監聽方向變化
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// 停止監聽
    /// </summary>
    void StopMonitoring();

    /// <summary>
    /// 監聽狀態
    /// </summary>
    bool IsMonitoring { get; }

    /// <summary>
    /// 方向變化事件 (備用直接訂閱方式)
    /// </summary>
    event EventHandler<OrientationChangedEventArgs>? OrientationChanged;
}

public record OrientationChangedEventArgs(
    ScreenOrientation PreviousOrientation,
    ScreenOrientation CurrentOrientation,
    DateTime Timestamp
);
```

#### 3.1.4 WMI 事件訂閱實現

```csharp
namespace WallpaperRotator.Infrastructure.Windows;

public sealed class OrientationDetector : IOrientationDetector
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<OrientationDetector> _logger;
    private ManagementEventWatcher? _watcher;
    private ScreenOrientation _lastOrientation;

    private const string WmiQuery =
        "SELECT * FROM __InstanceModificationEvent " +
        "WITHIN 1 " +
        "WHERE TargetInstance ISA 'Win32_DesktopMonitor'";

    public OrientationDetector(IEventBus eventBus, ILogger<OrientationDetector> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
        _lastOrientation = GetCurrentOrientation();
    }

    public ScreenOrientation GetCurrentOrientation()
    {
        int width = NativeMethods.GetSystemMetrics(NativeMethods.SM_CXSCREEN);
        int height = NativeMethods.GetSystemMetrics(NativeMethods.SM_CYSCREEN);

        return width > height
            ? ScreenOrientation.Landscape
            : ScreenOrientation.Portrait;
    }

    public void StartMonitoring()
    {
        if (IsMonitoring) return;

        try
        {
            _watcher = new ManagementEventWatcher(new WqlEventQuery(WmiQuery));
            _watcher.EventArrived += OnWmiEventArrived;
            _watcher.Start();

            IsMonitoring = true;
            _logger.LogInformation("Orientation monitoring started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start WMI watcher, falling back to timer");
            StartFallbackMonitoring();
        }
    }

    private void OnWmiEventArrived(object sender, EventArrivedEventArgs e)
    {
        var current = GetCurrentOrientation();

        if (current != _lastOrientation)
        {
            var previous = _lastOrientation;
            _lastOrientation = current;

            var evt = new OrientationChangedEvent(previous, current, DateTime.UtcNow);
            _eventBus.Publish(evt);

            OrientationChanged?.Invoke(this, new OrientationChangedEventArgs(
                previous, current, DateTime.UtcNow));
        }
    }

    // ... 其他實現
}
```

### 3.2 桌布套用器 (WallpaperApplier)

#### 3.2.1 介面定義

```csharp
namespace WallpaperRotator.Core.Interfaces;

public interface IWallpaperApplier
{
    /// <summary>
    /// 套用桌布
    /// </summary>
    /// <param name="imagePath">圖片完整路徑</param>
    /// <param name="displayMode">顯示模式</param>
    /// <param name="backgroundColor">背景顏色 (Fit 模式留白區)</param>
    /// <returns>套用結果</returns>
    Task<WallpaperApplyResult> ApplyAsync(
        string imagePath,
        DisplayMode displayMode = DisplayMode.Fit,
        string backgroundColor = "#000000");

    /// <summary>
    /// 取得當前桌布路徑
    /// </summary>
    string? GetCurrentWallpaperPath();

    /// <summary>
    /// 驗證圖片是否可用作桌布
    /// </summary>
    bool ValidateImage(string imagePath, out string? errorMessage);
}

public record WallpaperApplyResult(
    bool Success,
    string? ErrorMessage = null,
    TimeSpan ApplyDuration = default
);
```

#### 3.2.2 Windows API 整合

```csharp
namespace WallpaperRotator.Infrastructure.Windows;

internal static partial class NativeMethods
{
    // 螢幕尺寸
    public const int SM_CXSCREEN = 0;
    public const int SM_CYSCREEN = 1;

    // 桌布設定
    public const int SPI_SETDESKWALLPAPER = 0x0014;
    public const int SPI_GETDESKWALLPAPER = 0x0073;
    public const int SPIF_UPDATEINIFILE = 0x01;
    public const int SPIF_SENDCHANGE = 0x02;

    [LibraryImport("user32.dll")]
    public static partial int GetSystemMetrics(int nIndex);

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW",
        StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SystemParametersInfo(
        int uAction,
        int uParam,
        string lpvParam,
        int fuWinIni);

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW",
        StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SystemParametersInfoGet(
        int uAction,
        int uParam,
        [Out] char[] lpvParam,
        int fuWinIni);
}
```

#### 3.2.3 顯示模式對照表

```csharp
namespace WallpaperRotator.Core.Enums;

public enum DisplayMode
{
    /// <summary>
    /// 置中 (WallpaperStyle=0, TileWallpaper=0)
    /// </summary>
    Center = 0,

    /// <summary>
    /// 並排 (WallpaperStyle=0, TileWallpaper=1)
    /// </summary>
    Tile = 1,

    /// <summary>
    /// 延展 (WallpaperStyle=2, TileWallpaper=0)
    /// </summary>
    Stretch = 2,

    /// <summary>
    /// 符合 - 保持比例，可能留白 (WallpaperStyle=6, TileWallpaper=0)
    /// </summary>
    Fit = 6,

    /// <summary>
    /// 填滿 - 保持比例，可能裁切 (WallpaperStyle=10, TileWallpaper=0)
    /// </summary>
    Fill = 10,

    /// <summary>
    /// 跨螢幕 (WallpaperStyle=22, TileWallpaper=0)
    /// </summary>
    Span = 22
}
```

### 3.3 應用協調器 (AppCoordinator)

#### 3.3.1 職責

- 管理服務生命週期
- 協調各服務間的互動
- 作為事件總線的中央調度器
- 處理應用程式狀態變更

#### 3.3.2 事件流程

```
┌──────────────────────────────────────────────────────────────────────┐
│                         事件處理流程                                  │
├──────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  ┌───────────────┐                                                   │
│  │ 螢幕旋轉      │                                                   │
│  │ (硬體事件)    │                                                   │
│  └───────┬───────┘                                                   │
│          │                                                            │
│          ▼                                                            │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │ OrientationDetector.OnWmiEventArrived()                        │  │
│  │   → GetCurrentOrientation()                                    │  │
│  │   → 比較是否變化                                               │  │
│  └───────────────────────────┬───────────────────────────────────┘  │
│                              │                                        │
│                              ▼                                        │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │ EventBus.Publish(OrientationChangedEvent)                      │  │
│  └───────────────────────────┬───────────────────────────────────┘  │
│                              │                                        │
│          ┌───────────────────┼───────────────────┐                   │
│          ▼                   ▼                   ▼                   │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐           │
│  │ AppCoordinator│  │ TrayIconVM    │  │ Logger        │           │
│  │ .Handle()     │  │ .Handle()     │  │ .Handle()     │           │
│  └───────┬───────┘  └───────────────┘  └───────────────┘           │
│          │          (更新圖示狀態)     (記錄事件)                    │
│          ▼                                                            │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │ WallpaperService.SwitchForOrientation(orientation)             │  │
│  │   → ConfigStore.GetWallpaperFor(orientation)                   │  │
│  │   → WallpaperApplier.ApplyAsync(path, mode)                    │  │
│  └───────────────────────────┬───────────────────────────────────┘  │
│                              │                                        │
│                              ▼                                        │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │ EventBus.Publish(WallpaperAppliedEvent)                        │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                       │
└──────────────────────────────────────────────────────────────────────┘
```

#### 3.3.3 實現

```csharp
namespace WallpaperRotator.Application.Coordinators;

public sealed class AppCoordinator : IHostedService, IDisposable
{
    private readonly IOrientationDetector _orientationDetector;
    private readonly IWallpaperService _wallpaperService;
    private readonly IConfigurationStore _configStore;
    private readonly IEventBus _eventBus;
    private readonly ILogger<AppCoordinator> _logger;

    private IDisposable? _orientationSubscription;
    private bool _isEnabled = true;

    public AppCoordinator(
        IOrientationDetector orientationDetector,
        IWallpaperService wallpaperService,
        IConfigurationStore configStore,
        IEventBus eventBus,
        ILogger<AppCoordinator> logger)
    {
        _orientationDetector = orientationDetector;
        _wallpaperService = wallpaperService;
        _configStore = configStore;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AppCoordinator starting...");

        // 訂閱方向變更事件
        _orientationSubscription = _eventBus
            .Subscribe<OrientationChangedEvent>(HandleOrientationChanged);

        // 開始監控
        _orientationDetector.StartMonitoring();

        // 初始套用當前方向對應的桌布
        var currentOrientation = _orientationDetector.GetCurrentOrientation();
        await _wallpaperService.SwitchForOrientationAsync(currentOrientation);

        _logger.LogInformation("AppCoordinator started. Current orientation: {Orientation}",
            currentOrientation);
    }

    private async void HandleOrientationChanged(OrientationChangedEvent evt)
    {
        if (!_isEnabled)
        {
            _logger.LogDebug("Orientation changed but service is disabled");
            return;
        }

        _logger.LogInformation(
            "Orientation changed: {Previous} → {Current}",
            evt.PreviousOrientation,
            evt.CurrentOrientation);

        try
        {
            await _wallpaperService.SwitchForOrientationAsync(evt.CurrentOrientation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch wallpaper");
        }
    }

    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        _logger.LogInformation("Service {Status}", enabled ? "enabled" : "disabled");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _orientationDetector.StopMonitoring();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _orientationSubscription?.Dispose();
        _orientationDetector.Dispose();
    }
}
```

---

## 4. 配置管理設計

### 4.1 配置模型

```csharp
namespace WallpaperRotator.Application.Configuration;

public sealed record AppConfiguration
{
    public string Version { get; init; } = "2.0";
    public GeneralSettings Settings { get; init; } = new();
    public WallpaperSettings Wallpapers { get; init; } = new();
    public ScheduleSettings Schedule { get; init; } = new();
}

public sealed record GeneralSettings
{
    public bool Enabled { get; init; } = true;
    public bool StartWithWindows { get; init; } = true;
    public bool MinimizeToTray { get; init; } = true;
    public DisplayMode DisplayMode { get; init; } = DisplayMode.Fit;
    public string BackgroundColor { get; init; } = "#000000";
    public int TransitionDurationMs { get; init; } = 200;
    public string Language { get; init; } = "auto";
}

public sealed record WallpaperSettings
{
    public OrientationWallpapers Landscape { get; init; } = new();
    public OrientationWallpapers Portrait { get; init; } = new();
}

public sealed record OrientationWallpapers
{
    public RotationMode Mode { get; init; } = RotationMode.Single;
    public IReadOnlyList<string> Images { get; init; } = Array.Empty<string>();
    public int RotateIntervalSeconds { get; init; } = 0;
    public int CurrentIndex { get; init; } = 0;
}

public sealed record ScheduleSettings
{
    public bool Enabled { get; init; } = false;
    public IReadOnlyList<ScheduleRule> Rules { get; init; } = Array.Empty<ScheduleRule>();
}

public sealed record ScheduleRule
{
    public string Name { get; init; } = string.Empty;
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public WallpaperSettings Wallpapers { get; init; } = new();
}
```

### 4.2 配置儲存

```csharp
namespace WallpaperRotator.Core.Interfaces;

public interface IConfigurationStore
{
    /// <summary>
    /// 載入配置
    /// </summary>
    Task<AppConfiguration> LoadAsync();

    /// <summary>
    /// 儲存配置
    /// </summary>
    Task SaveAsync(AppConfiguration configuration);

    /// <summary>
    /// 配置變更事件
    /// </summary>
    event EventHandler<AppConfiguration>? ConfigurationChanged;

    /// <summary>
    /// 重設為預設值
    /// </summary>
    Task ResetToDefaultAsync();

    /// <summary>
    /// 匯出配置
    /// </summary>
    Task ExportAsync(string filePath);

    /// <summary>
    /// 匯入配置
    /// </summary>
    Task<AppConfiguration> ImportAsync(string filePath);
}
```

### 4.3 配置檔案位置

```
%LocalAppData%\WallpaperRotator\
├── config.json              # 主配置檔
├── config.json.backup       # 自動備份
└── logs/
    └── wallpaperrotator-{date}.log
```

---

## 5. 表現層設計

### 5.1 系統托盤

#### 5.1.1 狀態圖示

| 狀態 | 圖示 | 說明 |
|------|------|------|
| 運行中 (橫向) | 🖼️ (藍色) | 當前為橫向模式 |
| 運行中 (直向) | 🖼️ (綠色) | 當前為直向模式 |
| 已暫停 | ⏸️ (灰色) | 服務已暫停 |
| 錯誤 | ⚠️ (紅色) | 發生錯誤 |

#### 5.1.2 ViewModel

```csharp
namespace WallpaperRotator.Presentation.ViewModels;

public sealed class TrayIconViewModel : ViewModelBase, IDisposable
{
    private readonly AppCoordinator _coordinator;
    private readonly IEventBus _eventBus;

    private bool _isEnabled = true;
    private ScreenOrientation _currentOrientation;
    private string _statusText = "WallpaperRotator";

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

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public ICommand ToggleEnabledCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand SwitchToLandscapeCommand { get; }
    public ICommand SwitchToPortraitCommand { get; }
    public ICommand ExitCommand { get; }

    public TrayIconViewModel(AppCoordinator coordinator, IEventBus eventBus)
    {
        _coordinator = coordinator;
        _eventBus = eventBus;

        ToggleEnabledCommand = new RelayCommand(() => IsEnabled = !IsEnabled);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        SwitchToLandscapeCommand = new AsyncRelayCommand(SwitchToLandscape);
        SwitchToPortraitCommand = new AsyncRelayCommand(SwitchToPortrait);
        ExitCommand = new RelayCommand(Exit);

        _eventBus.Subscribe<OrientationChangedEvent>(OnOrientationChanged);
    }

    private void OnOrientationChanged(OrientationChangedEvent evt)
    {
        _currentOrientation = evt.CurrentOrientation;
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        StatusText = _isEnabled
            ? $"WallpaperRotator - {_currentOrientation}"
            : "WallpaperRotator - 已暫停";
    }

    // ... 其他實現
}
```

### 5.2 設定視窗

#### 5.2.1 導覽結構

```
SettingsWindow
├── NavigationView
│   ├── 一般設定 (GeneralSettingsPage)
│   ├── 桌布設定 (WallpaperSettingsPage)
│   ├── 排程設定 (ScheduleSettingsPage)
│   ├── 進階設定 (AdvancedSettingsPage)
│   └── 關於 (AboutPage)
└── ContentFrame
```

#### 5.2.2 MVVM 結構

```csharp
namespace WallpaperRotator.Presentation.ViewModels;

public sealed class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationStore _configStore;
    private AppConfiguration _configuration;
    private bool _hasUnsavedChanges;

    public GeneralSettingsViewModel General { get; }
    public WallpaperSettingsViewModel Wallpapers { get; }
    public ScheduleSettingsViewModel Schedule { get; }

    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set => SetProperty(ref _hasUnsavedChanges, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetCommand { get; }

    public SettingsViewModel(IConfigurationStore configStore)
    {
        _configStore = configStore;

        General = new GeneralSettingsViewModel(this);
        Wallpapers = new WallpaperSettingsViewModel(this);
        Schedule = new ScheduleSettingsViewModel(this);

        SaveCommand = new AsyncRelayCommand(SaveAsync, () => HasUnsavedChanges);
        CancelCommand = new RelayCommand(Cancel);
        ResetCommand = new AsyncRelayCommand(ResetAsync);

        PropertyChanged += (_, _) => HasUnsavedChanges = true;
    }

    public async Task LoadAsync()
    {
        _configuration = await _configStore.LoadAsync();
        ApplyConfiguration(_configuration);
        HasUnsavedChanges = false;
    }

    private async Task SaveAsync()
    {
        var newConfig = BuildConfiguration();
        await _configStore.SaveAsync(newConfig);
        _configuration = newConfig;
        HasUnsavedChanges = false;
    }

    // ... 其他實現
}
```

---

## 6. 依賴注入配置

### 6.1 服務註冊

```csharp
namespace WallpaperRotator;

public static class ServiceConfiguration
{
    public static IServiceCollection AddWallpaperRotatorServices(
        this IServiceCollection services)
    {
        // Core Services
        services.AddSingleton<IEventBus, InMemoryEventBus>();

        // Infrastructure Services
        services.AddSingleton<IOrientationDetector, OrientationDetector>();
        services.AddSingleton<IWallpaperApplier, WallpaperApplier>();
        services.AddSingleton<IConfigurationStore, JsonConfigurationStore>();
        services.AddSingleton<IAutoStartManager, AutoStartManager>();

        // Application Services
        services.AddSingleton<IWallpaperService, WallpaperService>();
        services.AddSingleton<ISchedulerService, SchedulerService>();
        services.AddSingleton<AppCoordinator>();

        // Hosted Service
        services.AddHostedService(sp => sp.GetRequiredService<AppCoordinator>());

        // ViewModels
        services.AddTransient<TrayIconViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<SetupWizardViewModel>();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddFile(GetLogPath(), LogLevel.Information);
        });

        return services;
    }

    private static string GetLogPath()
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "WallpaperRotator", "logs",
            $"wallpaperrotator-{DateTime.Now:yyyyMMdd}.log");
    }
}
```

### 6.2 應用程式入口

```csharp
namespace WallpaperRotator;

public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddWallpaperRotatorServices();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var configStore = _host.Services.GetRequiredService<IConfigurationStore>();
        var config = await configStore.LoadAsync();

        // 首次運行顯示設定精靈
        if (IsFirstRun())
        {
            var wizard = _host.Services.GetRequiredService<SetupWizardWindow>();
            wizard.ShowDialog();
        }

        // 初始化系統托盤
        var trayIcon = _host.Services.GetRequiredService<TrayIconView>();
        trayIcon.Initialize();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }

    private bool IsFirstRun()
    {
        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WallpaperRotator", "config.json");
        return !File.Exists(configPath);
    }
}
```

---

## 7. 測試策略

### 7.1 測試分層

```
┌─────────────────────────────────────────────────────────────┐
│                        測試金字塔                            │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│                      ┌─────────┐                            │
│                      │   E2E   │  ← UI 自動化測試           │
│                      │   10%   │    (Playwright/Appium)     │
│                   ┌──┴─────────┴──┐                         │
│                   │  Integration  │  ← 整合測試              │
│                   │     20%       │    (真實 Windows API)    │
│               ┌───┴───────────────┴───┐                     │
│               │       Unit Tests      │  ← 單元測試          │
│               │         70%           │    (Mock 依賴)       │
│               └───────────────────────┘                     │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 7.2 測試案例

```csharp
namespace WallpaperRotator.Application.Tests;

public class OrientationServiceTests
{
    private readonly Mock<IOrientationDetector> _detectorMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly OrientationService _sut;

    public OrientationServiceTests()
    {
        _detectorMock = new Mock<IOrientationDetector>();
        _eventBusMock = new Mock<IEventBus>();
        _sut = new OrientationService(_detectorMock.Object, _eventBusMock.Object);
    }

    [Fact]
    public void GetCurrentOrientation_WhenWidthGreaterThanHeight_ReturnsLandscape()
    {
        // Arrange
        _detectorMock
            .Setup(d => d.GetCurrentOrientation())
            .Returns(ScreenOrientation.Landscape);

        // Act
        var result = _sut.GetCurrentOrientation();

        // Assert
        Assert.Equal(ScreenOrientation.Landscape, result);
    }

    [Fact]
    public void StartMonitoring_ShouldStartDetector()
    {
        // Act
        _sut.StartMonitoring();

        // Assert
        _detectorMock.Verify(d => d.StartMonitoring(), Times.Once);
    }

    [Theory]
    [InlineData(ScreenOrientation.Landscape, ScreenOrientation.Portrait)]
    [InlineData(ScreenOrientation.Portrait, ScreenOrientation.Landscape)]
    public void WhenOrientationChanges_ShouldPublishEvent(
        ScreenOrientation from,
        ScreenOrientation to)
    {
        // Arrange
        var evt = new OrientationChangedEvent(from, to, DateTime.UtcNow);

        // Act
        _detectorMock.Raise(d => d.OrientationChanged += null,
            new OrientationChangedEventArgs(from, to, DateTime.UtcNow));

        // Assert
        _eventBusMock.Verify(
            e => e.Publish(It.Is<OrientationChangedEvent>(
                x => x.PreviousOrientation == from && x.CurrentOrientation == to)),
            Times.Once);
    }
}
```

### 7.3 覆蓋率目標

| 模組 | 目標覆蓋率 | 說明 |
|------|------------|------|
| Core | ≥ 90% | 領域模型、事件 |
| Application | ≥ 85% | 業務邏輯 |
| Infrastructure | ≥ 70% | 部分依賴真實 Windows API |
| Presentation | ≥ 60% | UI 測試較複雜 |

---

## 8. 部署架構

### 8.1 打包方式

| 方式 | 用途 | 優點 | 缺點 |
|------|------|------|------|
| **MSIX** | Microsoft Store | 自動更新、沙箱安全 | 需要簽章 |
| **Portable** | GitHub Release | 無需安裝、便攜 | 無自動更新 |
| **Inno Setup** | 傳統安裝 | 用戶習慣 | 需維護腳本 |

### 8.2 發布流程

```
┌─────────────────────────────────────────────────────────────┐
│                        CI/CD 流程                            │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐  │
│  │  Push   │───▶│  Build  │───▶│  Test   │───▶│ Package │  │
│  │ to main │    │ (.NET)  │    │ (xUnit) │    │ (MSIX)  │  │
│  └─────────┘    └─────────┘    └─────────┘    └────┬────┘  │
│                                                     │       │
│       ┌─────────────────────────────────────────────┘       │
│       │                                                      │
│       ▼                                                      │
│  ┌─────────┐    ┌─────────┐    ┌─────────────────────────┐ │
│  │  Sign   │───▶│ Publish │───▶│ Notify (GitHub Release) │ │
│  │ (Code)  │    │ (Store) │    │                         │ │
│  └─────────┘    └─────────┘    └─────────────────────────┘ │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### 8.3 GitHub Actions 配置

```yaml
name: Build and Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Test
        run: dotnet test --no-build --verbosity normal

      - name: Publish
        run: |
          dotnet publish src/WallpaperRotator/WallpaperRotator.csproj `
            -c Release `
            -r win-x64 `
            --self-contained true `
            -p:PublishSingleFile=true `
            -o ./publish/x64

          dotnet publish src/WallpaperRotator/WallpaperRotator.csproj `
            -c Release `
            -r win-arm64 `
            --self-contained true `
            -p:PublishSingleFile=true `
            -o ./publish/arm64

      - name: Create MSIX Package
        run: |
          # MSIX 打包腳本

      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            ./publish/x64/WallpaperRotator.exe
            ./publish/arm64/WallpaperRotator.exe
            ./publish/WallpaperRotator.msix
```

---

## 9. 安全考量

### 9.1 權限需求

| 權限 | 必要性 | 用途 |
|------|--------|------|
| 檔案系統讀取 | 必要 | 讀取桌布圖片 |
| 註冊表寫入 (HKCU) | 必要 | 設定桌布樣式 |
| 開機自啟動 | 可選 | 使用者授權後 |
| 網路存取 | 無需 | 完全離線運作 |

### 9.2 安全措施

```csharp
// 路徑驗證 - 防止路徑遍歷攻擊
public static bool IsValidImagePath(string path)
{
    if (string.IsNullOrWhiteSpace(path))
        return false;

    // 確保是絕對路徑
    if (!Path.IsPathFullyQualified(path))
        return false;

    // 確保檔案存在
    if (!File.Exists(path))
        return false;

    // 驗證副檔名
    var ext = Path.GetExtension(path).ToLowerInvariant();
    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
    if (!allowedExtensions.Contains(ext))
        return false;

    // 確保路徑在允許的位置 (非系統目錄)
    var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
    if (path.StartsWith(systemRoot, StringComparison.OrdinalIgnoreCase))
        return false;

    return true;
}
```

---

## 10. 效能優化

### 10.1 啟動優化

| 策略 | 說明 | 預期效果 |
|------|------|----------|
| 延遲載入 UI | 設定視窗按需載入 | 啟動快 50% |
| AOT 編譯 | Native AOT (可選) | 啟動快 70% |
| 單檔案發布 | 減少 I/O | 啟動快 20% |

### 10.2 記憶體優化

```csharp
// 圖片預覽使用縮圖，避免載入完整圖片
public static BitmapImage LoadThumbnail(string path, int maxWidth = 200)
{
    var bitmap = new BitmapImage();
    bitmap.BeginInit();
    bitmap.UriSource = new Uri(path);
    bitmap.DecodePixelWidth = maxWidth;
    bitmap.CacheOption = BitmapCacheOption.OnLoad;
    bitmap.EndInit();
    bitmap.Freeze(); // 允許跨執行緒存取
    return bitmap;
}
```

### 10.3 CPU 優化

- 使用事件驅動取代輪詢
- WMI 查詢使用 `WITHIN 1` 減少查詢頻率
- 避免在事件處理中進行重複計算

---

## 11. 遷移計畫

### 11.1 從 v1.x 遷移

```csharp
public class ConfigurationMigrator
{
    public async Task<AppConfiguration> MigrateFromV1Async()
    {
        // 檢查是否存在 v1 配置
        var v1ScriptPath = @"C:\Wallpapers\WallpaperRotator.ps1";
        if (!File.Exists(v1ScriptPath))
            return null;

        // 解析 v1 PowerShell 腳本中的路徑
        var content = await File.ReadAllTextAsync(v1ScriptPath);
        var landscapePath = ExtractPath(content, "LandscapePath");
        var portraitPath = ExtractPath(content, "PortraitPath");

        // 建立 v2 配置
        return new AppConfiguration
        {
            Settings = new GeneralSettings(),
            Wallpapers = new WallpaperSettings
            {
                Landscape = new OrientationWallpapers
                {
                    Images = new[] { landscapePath }
                },
                Portrait = new OrientationWallpapers
                {
                    Images = new[] { portraitPath }
                }
            }
        };
    }

    private string ExtractPath(string content, string variableName)
    {
        var pattern = $@"\${variableName}\s*=\s*""([^""]+)""";
        var match = Regex.Match(content, pattern);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
```

---

## 12. 附錄

### 12.1 技術選型決策記錄

| 決策 | 選項 | 選擇 | 理由 |
|------|------|------|------|
| UI 框架 | WPF / WinUI 3 / MAUI | WinUI 3 | 現代 UI、Win10/11 原生支援 |
| 方向偵測 | 輪詢 / WMI / DisplayConfig | WMI | 事件驅動、低資源 |
| 配置格式 | JSON / YAML / XML | JSON | 簡單、.NET 原生支援 |
| 日誌框架 | Serilog / NLog / 內建 | Serilog | 結構化日誌、擴展性 |
| DI 容器 | Microsoft.Extensions | Microsoft.Extensions | .NET 標準、輕量 |

### 12.2 參考資料

- [WinUI 3 Documentation](https://docs.microsoft.com/windows/apps/winui/)
- [Windows API - SystemParametersInfo](https://docs.microsoft.com/windows/win32/api/winuser/nf-winuser-systemparametersinfow)
- [WMI Events](https://docs.microsoft.com/windows/win32/wmisdk/receiving-event-notifications)
- [.NET Source Generators for P/Invoke](https://github.com/microsoft/CsWin32)

### 12.3 變更記錄

| 版本 | 日期 | 作者 | 變更內容 |
|------|------|------|----------|
| 1.0 | 2026-01-30 | Architect Agent | 初始版本 |

---

## 13. 審批

| 角色 | 姓名 | 日期 | 簽章 |
|------|------|------|------|
| Architect | | | ☐ |
| Tech Lead | | | ☐ |
| Product Manager | | | ☐ |

---

*Document generated by AI Agent Team*
