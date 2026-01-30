---
index: 11
gemini_id: 10c661656eb6dd0a
synced_at: 2026-01-29T06:32:52.931Z
---

## Q

windows詢問我要選擇哪個應用程式開啟.ps1

## A

這是 Windows 的安全機制，預設情況下 `.ps1` 檔案是為了「編輯」而非「執行」。如果您直接把 `.ps1` 檔的捷徑放入啟動資料夾，Windows 開機時就會困惑並跳出這個詢問視窗。

請依照以下\*\*修正後的「步驟四」\*\*操作，我們要建立一個「帶有執行指令」的捷徑，這樣 Windows 就會乖乖執行它，不再詢問。

### 修正後的做法

請將原本的捷徑刪除，改用以下方式建立：

1.  在桌面空白處按右鍵 -> **新增** -> **捷徑**。
    
2.  在「輸入項目的位置」欄位中，複製並貼上以下指令（**請注意修改您的真實路徑**）：
    
    ```
    powershell.exe -ExecutionPolicy Bypass -File "C:\Wallpapers\AutoWallpaper.ps1"
    
    ```
    
3.  按 **下一步**。
    
4.  名稱隨意（例如 `AutoWallpaper`），按 **完成**。
    
5.  現在，請把這個**新建立的捷徑**，拖拉進 `shell:startup` 資料夾中。
    

這樣做之後，這個捷徑就是一個「標準的應用程式指令」，Windows 會直接執行 PowerShell 並載入您的腳本，不會再跳出詢問視窗了。

* * *

### 已更新的 Markdown 教程 (修正步驟四)

我將上述修正整合進教程中，您可以直接使用這份最終版：

# Windows 平板螢幕旋轉自動切換桌布教程 (純 PowerShell 版)

**適用機型**：Fujitsu Q7311 / Q738 及各類 Windows 10/11 平板電腦。

## 前言

Windows 原生功能並不支援「依據螢幕直向/橫向自動切換不同桌布」。在平板豎持時，原本的橫向桌布通常會被裁切或留白，影響美觀。

本教程透過一個**整合版 PowerShell 腳本**，達成以下效果：

1.  **自動偵測**：背景監控螢幕旋轉狀態。
    
2.  **自動換圖**：橫向用橫圖，直向用直圖。
    
3.  **完美適配**：強制使用「符合 (Fit)」模式，保留圖片完整比例，不足處補黑邊。
    
4.  **自我隱藏**：程式啟動後會自動隱藏視窗，無需額外掛載 VBScript。
    

* * *

## 步驟一：準備桌布圖片

⚠️ **重要提醒**：Windows API **不支援**直接讀取 `.jxl` (JPEG XL) 格式。若強行使用會導致螢幕全黑。

1.  請準備兩張圖片（橫向/直向）。
    
2.  **格式轉換**：請務必使用轉檔軟體（如 XnView, Squoosh.app）將圖片轉存為 **.png** (無損推薦) 或 **.jpg**。
    
    *   _切勿直接修改副檔名（例如把 image.jxl 改名 image.png），這無效且會導致黑畫面。_
        
3.  建議建立一個簡單路徑存放，例如 `C:\Wallpapers`。
    
    *   `C:\Wallpapers\Landscape.png` (橫向用)
        
    *   `C:\Wallpapers\Portrait.png` (直向用)
        

* * *

## 步驟二：設定 Windows 底色 (黑邊準備)

為了讓圖片在「符合 (Fit)」模式下，上下留白處呈現完美的黑色（而非預設藍色）：

1.  在桌面按右鍵 -> **個人化 (Personalize)**。
    
2.  點選 **背景 (Background)**。
    
3.  將背景選單暫時切換為 **純色 (Solid color)**。
    
4.  選擇 **黑色**。
    
5.  設定完成後，您可以關閉視窗（稍後腳本會接手圖片顯示）。
    

* * *

## 步驟三：建立自動切換腳本 (AutoWallpaper.ps1)

此腳本內建了「視窗隱藏」功能，執行時只會閃現一下即消失。

1.  在桌面或任意資料夾，新增一個文字文件。
    
2.  複製以下完整程式碼。
    
3.  **修改路徑**：請編輯程式碼中段的 `$LandscapePath` 與 `$PortraitPath`，填入您真實的圖片路徑。
    
4.  儲存檔案，並將檔名改為 `AutoWallpaper.ps1`。
    

```
# ==========================================
# 區塊 1: 自我隱藏視窗邏輯
# 程式啟動後會自動呼叫 API 隱藏自己的視窗
# ==========================================
$t = '[DllImport("user32.dll")] public static extern bool ShowWindow(int handle, int state);'
add-type -name win -member $t -namespace native
$hwnd = (Get-Process -Id $PID).MainWindowHandle
# 0 = Hide (隱藏), 1 = Normal (顯示)
[native.win]::ShowWindow($hwnd, 0)

# ==========================================
# 區塊 2: 桌布自動切換核心邏輯
# ==========================================
Add-Type @"
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

public class Wallpaper {
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);
}
"@

# ------------------------------------------
# 設定區：請修改圖片的絕對路徑 (必填)
# ------------------------------------------
$LandscapePath = "C:\Wallpapers\Landscape.png"
$PortraitPath  = "C:\Wallpapers\Portrait.png"

$SPI_SETDESKWALLPAPER = 0x0014
$UpdateIniFile = 0x01
$SendWinIniChange = 0x02
$CurrentState = "Unknown"

# 設定桌布樣式函數
# Value "6" 代表 "Fit (符合)"，圖片完整顯示不裁切，留黑邊
# Value "10" 代表 "Fill (填滿)"，圖片裁切放大
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

## 步驟四：設定開機自動執行 (建立執行捷徑)

為了避免 Windows 跳出「您要如何開啟此檔案」的詢問，我們必須建立一個包含執行指令的標準捷徑。

1.  在桌面空白處按右鍵 -> **新增** -> **捷徑**。
    
2.  在「輸入項目的位置」欄位中，複製並貼上以下指令： _(⚠️ 請注意：引號內的檔案路徑必須修改為您存放 ps1 檔的真實路徑)_
    
    ```
    powershell.exe -ExecutionPolicy Bypass -File "C:\Wallpapers\AutoWallpaper.ps1"
    
    ```
    
3.  按 **下一步**，名稱輸入 `AutoWallpaper`，按 **完成**。
    
4.  按下鍵盤 `Win + R`，輸入 `shell:startup` 並按 Enter。
    
5.  將剛剛建立在桌面的捷徑，拖進這個開啟的「啟動」資料夾中。
    

**完成！** 現在您可以雙擊捷徑測試（視窗會閃一下消失），接著旋轉您的平板，桌布應會隨之切換。下次重開機後，程式會自動在背景執行。

* * *

## 常見問題排查 (Troubleshooting)

**Q: 執行捷徑時出現紅字錯誤？**

*   請檢查步驟四指令中的路徑是否正確，特別是路徑如果有空白，確保有被雙引號包起來。
    

**Q: 執行後螢幕變成全黑？**

*   **原因 1**：圖片路徑錯誤。請檢查 `.ps1` 檔內的 `$LandscapePath` 路徑是否正確。
    
*   **原因 2**：圖片格式錯誤。請確認您不是直接把 `.jxl` 改名成 `.png`，而是真的經過轉檔軟體處理過。
    

**Q: 如何關閉這個程式？**

*   由於程式隱藏在背景，請開啟「工作管理員 (Task Manager)」，在「處理程序」或「詳細資料」分頁中找到 `Windows PowerShell`，按右鍵選擇「結束工作」即可。
