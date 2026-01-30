---
index: 7
gemini_id: 0e5f50b06a45fd22
synced_at: 2026-01-29T06:32:52.872Z
---

## Q

請將完整包含powershell指令、步驟、VBScript重新整理成一個markdown，我要做成教程分享

## A

這是一份完整的 Markdown 教程，您可以直接複製並分享給社群或儲存到 Obsidian/Notion 中使用。這份教程整合了我們討論過的所有細節：包含錯誤排查、格式轉換提醒以及全螢幕設定。

* * *

# Windows 平板螢幕旋轉自動切換桌布教程 (PowerShell 方案)

適用於 Fujitsu Q7311 及其他 Windows 10/11 平板電腦。

## 前言

Windows 原生設定並不支援「依據螢幕方向（橫向/直向）自動切換不同桌布」。在平板模式直立使用時，原本的橫向桌布通常會被裁切或留白，非常不美觀。

本教程將透過輕量級的 **PowerShell 腳本**，達成以下效果：

1.  **自動偵測**：背景自動監控螢幕旋轉。
    
2.  **自動換圖**：橫向用橫圖，直向用直圖。
    
3.  **完美適配**：強制使用「符合 (Fit)」模式，確保圖片完整顯示，不足處補黑邊。
    
4.  **靜默執行**：開機自動執行且不跳出視窗。
    

* * *

## 步驟一：準備桌布圖片

⚠️ **重要提醒**：Windows API **不支援**直接讀取 `.jxl` (JPEG XL) 格式。若強行使用會導致螢幕全黑。

1.  請準備兩張圖片（橫向/直向）。
    
2.  **格式轉換**：請務必使用轉檔軟體（如 XnView, Squoosh.app, 或 Mac 上的 `imagemagick`）將圖片轉存為 **.png** (無損推薦) 或 **.jpg**。
    
    *   _切勿直接修改副檔名（例如把 image.jxl 改名 image.png），這無效且會導致黑畫面。_
        
3.  建議建立一個簡單路徑存放，例如 `C:\Wallpapers`。
    
    *   `C:\Wallpapers\Landscape.png` (橫向用)
        
    *   `C:\Wallpapers\Portrait.png` (直向用)
        

* * *

## 步驟二：設定 Windows 底色 (黑邊準備)

為了讓圖片在「符合」模式下，留白處呈現完美的黑色（而非藍色或其他主題色）：

1.  在桌面按右鍵 -> **個人化 (Personalize)**。
    
2.  點選 **背景 (Background)**。
    
3.  將背景選單暫時切換為 **純色 (Solid color)**。
    
4.  選擇 **黑色**。
    
5.  設定完成後，您可以關閉視窗（稍後腳本會接手圖片顯示）。
    

* * *

## 步驟三：建立自動切換腳本 (AutoWallpaper.ps1)

1.  在桌面或任意資料夾，新增一個文字文件。
    
2.  複製以下程式碼。
    
3.  **修改路徑**：請編輯程式碼前段的 `$LandscapePath` 與 `$PortraitPath`，填入您真實的圖片路徑。
    
4.  儲存檔案，並將檔名改為 `AutoWallpaper.ps1`。
    

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
# 設定區：請修改圖片的絕對路徑
# ==========================================
$LandscapePath = "C:\Wallpapers\Landscape.png"
$PortraitPath  = "C:\Wallpapers\Portrait.png"

$SPI_SETDESKWALLPAPER = 0x0014
$UpdateIniFile = 0x01
$SendWinIniChange = 0x02
$CurrentState = "Unknown"

# 設定桌布樣式函數
# Value "6" 代表 "Fit (符合)"，圖片完整顯示不裁切
# Value "10" 代表 "Fill (填滿)"，圖片填滿螢幕但在直立時會裁切
function Set-WallpaperStyle {
    Set-ItemProperty -Path "HKCU:\Control Panel\Desktop" -Name WallpaperStyle -Value "6"
    Set-ItemProperty -Path "HKCU:\Control Panel\Desktop" -Name TileWallpaper -Value "0"
}

while ($true) {
    # 載入螢幕資訊
    Add-Type -AssemblyName System.Windows.Forms
    $Screen = [System.Windows.Forms.Screen]::PrimaryScreen
    $Width = $Screen.Bounds.Width
    $Height = $Screen.Bounds.Height
    
    # 判斷當前方向
    if ($Width -gt $Height) {
        $TargetFile = $LandscapePath
        $TargetState = "Landscape"
    } else {
        $TargetFile = $PortraitPath
        $TargetState = "Portrait"
    }

    # 若方向改變，則執行換圖
    if ($CurrentState -ne $TargetState) {
        
        # 安全檢查：確認檔案存在才執行，避免黑畫面
        if (Test-Path $TargetFile) {
            # 1. 先設定為 Fit 模式
            Set-WallpaperStyle
            
            # 2. 呼叫 Windows API 換圖
            [Wallpaper]::SystemParametersInfo($SPI_SETDESKWALLPAPER, 0, $TargetFile, $UpdateIniFile -bor $SendWinIniChange)
            
            $CurrentState = $TargetState
        }
    }
    
    # 每 2 秒檢查一次旋轉狀態
    Start-Sleep -Seconds 2
}

```

* * *

## 步驟四：設定開機自動隱藏執行 (StartWP.vbs)

直接執行 `.ps1` 檔案會跳出一個藍色的 PowerShell 視窗。我們使用 VBScript 來隱藏它。

1.  新增一個文字文件。
    
2.  複製以下代碼：
    

```
Set objShell = CreateObject("Wscript.Shell")
' 下方路徑請修改為您存放 AutoWallpaper.ps1 的真實路徑
objShell.Run "powershell.exe -ExecutionPolicy Bypass -File C:\Wallpapers\AutoWallpaper.ps1", 0

```

3.  儲存檔案，將檔名改為 `StartWP.vbs`。
    
4.  按下鍵盤 `Win + R`，輸入 `shell:startup` 並按 Enter。
    
5.  將 `StartWP.vbs` 檔案（或是它的捷徑）放入這個開啟的「啟動」資料夾中。
    

**完成！** 重開機後，程式將在背景默默運作，您可以試著旋轉您的 Fujitsu 平板來測試效果。

* * *

## 常見問題排查 (Troubleshooting)

**Q: 執行後螢幕變成全黑？**

*   **原因 1**：圖片路徑錯誤。請檢查 `.ps1` 檔內的路徑是否完全正確。
    
*   **原因 2**：圖片格式錯誤。請確認您不是直接把 `.jxl` 改名成 `.png`，而是真的經過轉檔軟體處理過。
    
*   **原因 3**：PowerShell 沒有讀取檔案的權限（極少見，通常發生在圖片放在系統保護資料夾時）。
    

**Q: 旋轉後反應有點慢？**

*   Fujitsu 的感測器驅動傳遞訊號需要時間，加上腳本設定每 2 秒檢查一次。您可以修改腳本最後一行的 `Start-Sleep -Seconds 2` 為 `1`，但不建議設為 0，會佔用 CPU 資源。
    

**Q: 如何停止程式？**

*   因為它是背景執行，請開啟「工作管理員」，找到 `Windows PowerShell`，按右鍵「結束工作」即可。若要永久移除，請將 `shell:startup` 資料夾內的 VBS 檔案刪除。
