---
index: 13
gemini_id: 2f0e3fe1bfb77315
synced_at: 2026-01-29T06:32:52.988Z
---

## Q

仍然有執行異常，開機時自動自行，powershell視窗不會隱藏，視窗會出現false 跟1字樣，轉動時，不一定會順利執行切換桌布，需要再手動執行一次ps1

## A

這是一個非常具體的回饋，感謝您。

**問題分析：**

1.  **出現 `False`**：是因為開機時腳本執行速度太快，視窗還沒完全「畫」出來，腳本就嘗試隱藏它，導致抓不到視窗控制代碼（Handle）而失敗。
    
2.  **出現 `1`**：是因為 `SystemParametersInfo` 這個 API 執行成功後會回傳 `1`，PowerShell 預設會把這個數字印在螢幕上。
    
3.  **不穩定/需手動執行**：可能是因為開機時多個腳本實例衝突，或是系統尚未準備好就開始偵測。
    

我已經修正了程式碼與步驟。這個**最終修正版 (V3)** 做了以下改進：

*   **強制靜音**：將所有 API 回傳值（那個 `1`）導入 `Out-Null`，確保視窗乾淨。
    
*   **延遲啟動**：加入 `Start-Sleep` 等待視窗載入後再執行隱藏，解決 `False` 問題。
    
*   **雙重保險**：在捷徑設定中直接加入 `-WindowStyle Hidden` 參數，這是微軟官方隱藏視窗最穩定的方法，不再單純依賴程式碼自隱藏。
    

請使用這份修正後的 Markdown 教程：

* * *

# Windows 平板螢幕旋轉自動切換桌布教程 (終極修正版)

**適用機型**：Fujitsu Q7311 / Q738 及各類 Windows 10/11 平板電腦。

## 前言

Windows 原生功能並不支援「依據螢幕直向/橫向自動切換不同桌布」。本教程透過 PowerShell 腳本達成：

1.  **自動偵測**：監控螢幕旋轉。
    
2.  **自動換圖**：橫向用橫圖，直向用直圖。
    
3.  **完美隱藏**：解決開機跳出視窗、顯示錯誤代碼的問題。
    

* * *

## 步驟一：準備桌布圖片

1.  準備兩張圖片（橫向/直向），建議使用 **.png** 或 **.jpg**。
    
2.  建立資料夾 `C:\Wallpapers` 並放入圖片：
    
    *   `C:\Wallpapers\Landscape.png`
        
    *   `C:\Wallpapers\Portrait.png`
        

* * *

## 步驟二：設定 Windows 底色 (黑邊準備)

為了讓圖片在「符合 (Fit)」模式下，留白處呈現完美黑色：

1.  桌面右鍵 -> **個人化** -> **背景**。
    
2.  將背景切換為 **純色**，選擇 **黑色**。
    
3.  設定完成後關閉視窗。
    

* * *

## 步驟三：建立自動切換腳本 (AutoWallpaper.ps1)

此版本修正了開機執行失敗與顯示亂碼的問題。

1.  在 `C:\Wallpapers` 資料夾中，新增文字文件。
    
2.  複製以下**完整修正程式碼**。
    
3.  儲存為 `AutoWallpaper.ps1`。
    

```
# ==========================================
# 修正版腳本：靜音執行 & 穩定隱藏
# ==========================================

# 1. 開機緩衝：等待 2 秒確保系統載入完畢，避免視窗抓取失敗
Start-Sleep -Seconds 2

# 2. 定義 Windows API
$t = @'
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;
public class Wallpaper {
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
}
[DllImport("user32.dll")]
public static extern bool ShowWindow(int handle, int state);
'@
Add-Type -Name win -Member $t -Namespace native

# 3. 執行視窗隱藏 (補強捷徑隱藏失效的情況)
$hwnd = (Get-Process -Id $PID).MainWindowHandle
if ($hwnd -ne 0) { 
    [native.win]::ShowWindow($hwnd, 0) | Out-Null 
}

# ------------------------------------------
# 設定區：請確認路徑正確
# ------------------------------------------
$LandscapePath = "C:\Wallpapers\Landscape.png"
$PortraitPath  = "C:\Wallpapers\Portrait.png"

$SPI_SETDESKWALLPAPER = 0x0014
$UpdateIniFile = 0x01
$SendWinIniChange = 0x02
$CurrentState = "Unknown"

# 設定桌布樣式函數 (Fit = 6)
function Set-WallpaperStyle {
    Set-ItemProperty -Path "HKCU:\Control Panel\Desktop" -Name WallpaperStyle -Value "6" | Out-Null
    Set-ItemProperty -Path "HKCU:\Control Panel\Desktop" -Name TileWallpaper -Value "0" | Out-Null
}

# 4. 主迴圈
while ($true) {
    try {
        Add-Type -AssemblyName System.Windows.Forms
        $Screen = [System.Windows.Forms.Screen]::PrimaryScreen
        $Width = $Screen.Bounds.Width
        $Height = $Screen.Bounds.Height
        
        if ($Width -gt $Height) {
            $TargetFile = $LandscapePath
            $TargetState = "Landscape"
        } else {
            $TargetFile = $PortraitPath
            $TargetState = "Portrait"
        }

        if ($CurrentState -ne $TargetState) {
            if (Test-Path $TargetFile) {
                Set-WallpaperStyle
                # [void] 用來強制不顯示回傳值 (解決出現 "1" 的問題)
                [void][Wallpaper]::SystemParametersInfo($SPI_SETDESKWALLPAPER, 0, $TargetFile, $UpdateIniFile -bor $SendWinIniChange)
                $CurrentState = $TargetState
            }
        }
    } catch {
        # 忽略錯誤，避免腳本崩潰停止
    }
    
    Start-Sleep -Seconds 2
}

```

* * *

## 步驟四：設定開機自動執行 (關鍵修正)

為了徹底隱藏視窗並確保開機順利執行，我們要在捷徑中加入官方隱藏參數。

1.  在桌面空白處按右鍵 -> **新增** -> **捷徑**。
    
2.  在「輸入項目的位置」欄位中，**完整複製並貼上**以下指令（請確認只有一行）：
    
    ```
    powershell.exe -WindowStyle Hidden -ExecutionPolicy Bypass -File "C:\Wallpapers\AutoWallpaper.ps1"
    
    ```
    *   _解釋：加入 `-WindowStyle Hidden` 是讓 PowerShell 一啟動就隱藏的官方指令。_
        
3.  按 **下一步**，名稱輸入 `AutoWallpaper`，按 **完成**。
    
4.  按下鍵盤 `Win + R`，輸入 `shell:startup` 並按 Enter。
    
5.  將剛剛建立在桌面的捷徑，拖進這個「啟動」資料夾中。
    
6.  **(建議)** 如果資料夾內有舊的捷徑或 VBS 檔案，請全部刪除，只留這一個新的。
    

* * *

## 驗證是否成功

1.  **重開機**。
    
2.  登入後，您應該**不會**看到任何藍色或黑色視窗（即便有也是一閃而過）。
    
3.  等待約 3~5 秒（腳本有設定緩衝時間）。
    
4.  旋轉平板，桌布應會自動切換。
