---
index: 4
gemini_id: 49b5cf15f7d1609e
synced_at: 2026-01-29T06:32:52.837Z
---

## Q

上述powershell語法是否考慮設置桌布時，以全螢幕模式？

## A

您非常敏銳，**原本的程式碼並沒有強制設定「顯示模式」**。

原本的語法 `SystemParametersInfo` 主要是負責「通知系統更換圖片檔案」，但它會直接沿用您 Windows 目前設定的桌布樣式（例如：如果您上次設定是「置中」，腳本換圖後依然會是「置中」，導致周圍留黑邊）。

要在腳本中強制設定為「全螢幕」，我們需要修改 Windows 的 **機碼 (Registry)**。

### 什麼是「全螢幕」？

在 Windows 定義中，您指的「全螢幕」通常有兩種：

1.  **填滿 (Fill) - 數值 10 (推薦)**：保持圖片比例放大，裁切掉多餘部分。這是最常用的模式，不會讓圖片變形。
    
2.  **延展 (Stretch) - 數值 2**：強制把圖片拉大到跟螢幕一樣大。如果圖片比例跟螢幕不同，**圖片會變形（變扁或變瘦）**。
    

### 修改後的完整程式碼 (加入強制「填滿」模式)

這段代碼加入了修改 `HKCU:\Control Panel\Desktop` 的步驟，確保每次切換都一定是「填滿」模式。

```
Add-Type @"
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

public class Wallpaper {
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
}
"@

# --- 設定圖片路徑 (記得改成 .png 或 .jpg) ---
$LandscapePath = "C:\Wallpapers\Landscape.png"
$PortraitPath = "C:\Wallpapers\Portrait.png"
# --------------------------------

$SPI_SETDESKWALLPAPER = 0x0014
$UpdateIniFile = 0x01
$SendWinIniChange = 0x02
$CurrentState = "Unknown"

# 定義修改桌布樣式的函數
function Set-WallpaperStyle {
    # WallpaperStyle: 2=Stretch(延展), 6=Fit(符合), 10=Fill(填滿)
    # TileWallpaper: 0=No Tile, 1=Tile
    
    # 這裡設為 "10" (填滿)，若您想要強制拉伸變形請改成 "2"
    Set-ItemProperty -Path "HKCU:\Control Panel\Desktop" -Name WallpaperStyle -Value "10"
    Set-ItemProperty -Path "HKCU:\Control Panel\Desktop" -Name TileWallpaper -Value "0"
}

while ($true) {
    Add-Type -AssemblyName System.Windows.Forms
    $Screen = [System.Windows.Forms.Screen]::PrimaryScreen
    $Width = $Screen.Bounds.Width
    $Height = $Screen.Bounds.Height

    if ($Width -gt $Height) {
        # 橫向模式
        if ($CurrentState -ne "Landscape") {
            Set-WallpaperStyle # 先設定樣式
            [Wallpaper]::SystemParametersInfo($SPI_SETDESKWALLPAPER, 0, $LandscapePath, $UpdateIniFile -bor $SendWinIniChange)
            $CurrentState = "Landscape"
        }
    } else {
        # 直向模式
        if ($CurrentState -ne "Portrait") {
            Set-WallpaperStyle # 先設定樣式
            [Wallpaper]::SystemParametersInfo($SPI_SETDESKWALLPAPER, 0, $PortraitPath, $UpdateIniFile -bor $SendWinIniChange)
            $CurrentState = "Portrait"
        }
    }
    # 每 2 秒檢查一次 (加快一點反應速度)
    Start-Sleep -Seconds 2
}

```

### 重點修改說明

1.  **新增 `Set-WallpaperStyle` 函數**： 它會去修改 `HKCU:\Control Panel\Desktop` 下的 `WallpaperStyle`。
    
    *   我預設設為 **"10" (Fill/填滿)**，這是平板旋轉時最自然的設定。
        
    *   如果您希望圖片完全不裁切，寧可變形也要塞滿畫面，請將代碼中的 `"10"` 改為 `"2"`。
        
2.  **執行順序**： 我在呼叫 `[Wallpaper]::SystemParametersInfo` **之前** 先執行了樣式設定，確保系統載入新圖片時，已經讀取到正確的「填滿」設定。
    

您可以直接用這個新版本覆蓋原本的 `AutoWallpaper.ps1` 檔案即可。
