# WallpaperRotator

Windows 平板螢幕旋轉自動切換桌布工具

## 功能特色

- 🔄 **即時偵測** - 使用 WMI 事件驅動偵測螢幕方向變化
- 🖼️ **自動換圖** - 橫向/直向自動切換對應桌布
- ✨ **完美適配** - 支援 Fit/Fill/Stretch 等多種顯示模式
- 👻 **背景執行** - 系統托盤運行，完全無感
- 🔋 **省電設計** - 事件驅動，CPU 佔用近乎為 0

## 適用設備

- Fujitsu Q7311 / Q738
- Microsoft Surface 系列
- 其他 Windows 10/11 平板電腦

## 系統需求

- Windows 10 1903+ 或 Windows 11
- .NET 8.0 Runtime

## 安裝

從 [Releases](../../releases) 下載最新版本。

## 開發

### 環境需求

- .NET 8.0 SDK
- Visual Studio 2022 或 VS Code

### 編譯

```bash
cd src
dotnet restore
dotnet build
```

### 發布

```bash
dotnet publish src/WallpaperRotator/WallpaperRotator.csproj -c Release -r win-x64 --self-contained
```

## 授權

MIT License
