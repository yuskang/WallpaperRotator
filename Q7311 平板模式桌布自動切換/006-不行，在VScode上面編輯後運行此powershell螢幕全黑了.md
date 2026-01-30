---
index: 6
gemini_id: f3795c561dfda490
synced_at: 2026-01-29T06:32:52.860Z
---

## Q

不行，在VScode上面編輯後運行此powershell螢幕全黑了

## A

這是一個典型的「找不到檔案」或「檔案格式錯誤」的現象。因為我們剛才將底色設為了黑色，**當 Windows 找不到指定的圖片，或者圖片格式無法讀取時，它就會直接顯示底色（全黑）。**

請不要驚慌，這不是系統壞掉。請依照以下步驟排查並修正：

### 1\. 緊急復原 (先讓畫面回來)

既然現在全黑，請先手動恢復一張桌布：

*   在全黑的桌面上按 **右鍵** -> **個人化** -> **背景** -> 隨便選一張 Windows 內建圖片。
    

* * *

### 2\. 關鍵排查：您是真的「轉檔」還是只有「改檔名」？

這是最常見的錯誤原因。

*   **錯誤做法**：直接在檔案總管把 `image.jxl` 重新命名為 `image.png`。
    
    *   結果：Windows 依然把它當作 PNG 讀取，但內部編碼還是 JXL，導致讀取失敗 -> **黑畫面**。
        
*   **正確做法**：必須使用轉檔工具（如 Squoosh 或 XnView）重新存檔。
    

**測試方法**：試著用 Windows 內建的「相片」App 或「小畫家」打開那張 PNG。如果打不開，就是檔案有問題。

* * *

### 3\. 路徑錯誤排查 (VS Code 常見問題)

在 VS Code 中執行 PowerShell 時，`./` (當前目錄) 可能不是您想像的位置。

請使用下方這個**帶有「除錯功能」的修復版腳本**。它會在執行時檢查檔案是否存在，如果找不到，它會在 VS Code 下方的終端機印出**紅色的錯誤訊息**告訴您。

#### 修復版腳本 (加入路徑檢查與錯誤提示)

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

# ==========================================
# 請務必確認這裡的路徑是「絕對路徑」
# 例如: "C:\Users\您的名字\Pictures\Wallpapers\Landscape.png"
# 不要使用 ".\Landscape.png"
# ==========================================
$LandscapePath = "C:\Wallpapers\Landscape.png"
$PortraitPath = "C:\Wallpapers\Portrait.png"

$SPI_SETDESKWALLPAPER = 0x0014
$UpdateIniFile = 0x01
$SendWinIniChange = 0x02
$CurrentState = "Unknown"

# 設定桌布樣式函數 (Fit = 6)
function Set-WallpaperStyle {
    Set-ItemProperty -Path "HKCU:\Control Panel\Desktop" -Name WallpaperStyle -Value "6"
    Set-ItemProperty -Path "HKCU:\Control Panel\Desktop" -Name TileWallpaper -Value "0"
}

Write-Host "腳本開始運行... 按 Ctrl+C 停止" -ForegroundColor Cyan

while ($true) {
    Add-Type -AssemblyName System.Windows.Forms
    $Screen = [System.Windows.Forms.Screen]::PrimaryScreen
    $Width = $Screen.Bounds.Width
    $Height = $Screen.Bounds.Height
    
    # 決定要用哪張圖
    if ($Width -gt $Height) {
        $TargetFile = $LandscapePath
        $TargetState = "Landscape"
    } else {
        $TargetFile = $PortraitPath
        $TargetState = "Portrait"
    }

    # 狀態改變時才執行動作
    if ($CurrentState -ne $TargetState) {
        
        # --- 除錯檢查：確認檔案真的存在 ---
        if (Test-Path $TargetFile) {
            Write-Host "偵測到 $TargetState 模式，正在切換至: $TargetFile" -ForegroundColor Green
            
            Set-WallpaperStyle
            
            # 執行切換
            $Result = [Wallpaper]::SystemParametersInfo($SPI_SETDESKWALLPAPER, 0, $TargetFile, $UpdateIniFile -bor $SendWinIniChange)
            
            # 如果 API 回傳 0 表示失敗
            if ($Result -eq 0) {
                Write-Host "錯誤：Windows API 呼叫失敗，請檢查檔案是否被鎖定。" -ForegroundColor Red
            } else {
                $CurrentState = $TargetState
            }
        } else {
            # 這裡就是為什麼螢幕會黑掉的原因
            Write-Host "嚴重錯誤：找不到檔案！" -ForegroundColor Red
            Write-Host "系統試圖讀取: $TargetFile" -ForegroundColor Red
            Write-Host "請檢查路徑是否正確，副檔名是否正確。" -ForegroundColor Yellow
        }
    }
    
    Start-Sleep -Seconds 2
}

```

### 執行步驟：

1.  將上述代碼複製到 VS Code。
    
2.  **非常重要**：仔細檢查 `$LandscapePath` 和 `$PortraitPath`，確保裡面是 `C:\...` 開頭的完整路徑，不要用相對路徑。
    
3.  執行腳本。
    
4.  **觀察 VS Code 下方的終端機 (Terminal)**：
    
    *   如果是綠字：表示路徑正確，腳本已送出指令。
        
    *   如果是紅字 `嚴重錯誤：找不到檔案！`：請修正路徑。
        
    *   如果是綠字但螢幕還是黑的：**那是圖片格式問題**（請回到第 2 點檢查轉檔是否成功）。
