---
index: 16
gemini_id: ea894e9fa1a8bd1f
synced_at: 2026-01-29T06:32:53.067Z
---

## Q

Add-Type : c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(8) : 類別、結構或介面成員宣告中無效的語彙基元 'using'

c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(7) :     {

c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(8) : >>>     using System;

c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(9) : using System.Runtime.InteropServices;

位於 C:\Wallpapers\AutoWallpaper.ps1:26 字元:1

+ Add-Type -Name win -Member $t -Namespace native

+ ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    + CategoryInfo          : InvalidData: (Microsoft.Power...peCompilerError:AddTypeCompilerError) [Add-Type]，Excepti

    on

    + FullyQualifiedErrorId : SOURCE_CODE_ERROR,Microsoft.PowerShell.Commands.AddTypeCommand




Add-Type : c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(8) : 類別、結構或介面成員宣告中無效的語彙基元 ';'

c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(7) :     {

c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(8) : >>>     using System;

c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(9) : using System.Runtime.InteropServices;

位於 C:\Wallpapers\AutoWallpaper.ps1:26 字元:1

+ Add-Type -Name win -Member $t -Namespace native

+ ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    + CategoryInfo          : InvalidData: (Microsoft.Power...peCompilerError:AddTypeCompilerError) [Add-Type]，Excepti

    on

    + FullyQualifiedErrorId : SOURCE_CODE_ERROR,Microsoft.PowerShell.Commands.AddTypeCommand




Add-Type : c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(9) : 類別、結構或介面成員宣告中無效的語彙基元 ';'

c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(8) :     using System;

c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(9) : >>> using System.Runtime.InteropServices;

c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(10) : using Microsoft.Win32;

位於 C:\Wallpapers\AutoWallpaper.ps1:26 字元:1

+ Add-Type -Name win -Member $t -Namespace native

+ ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    + CategoryInfo          : InvalidData: (Microsoft.Power...peCompilerError:AddTypeCompilerError) [Add-Type]，Excepti

    on

    + FullyQualifiedErrorId : SOURCE_CODE_ERROR,Microsoft.PowerShell.Commands.AddTypeCommand




Add-Type : c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(10) : 類別、結構或介面成員宣告中無效的語彙基元 ';'

c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(9) : using System.Runtime.InteropServices;

c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(10) : >>> using Microsoft.Win32;

c:\Users\Q7311\AppData\Local\Temp\z1yfjfjb.0.cs(11) : public class Wallpaper {

位於 C:\Wallpapers\AutoWallpaper.ps1:26 字元:1

+ Add-Type -Name win -Member $t -Namespace native

+ ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    + CategoryInfo          : InvalidData: (Microsoft.Power...peCompilerError:AddTypeCompilerError) [Add-Type]，Excepti

    on

    + FullyQualifiedErrorId : SOURCE_CODE_ERROR,Microsoft.PowerShell.Commands.AddTypeCommand




Add-Type : 無法新增類型。發生編譯錯誤。

位於 C:\Wallpapers\AutoWallpaper.ps1:26 字元:1

+ Add-Type -Name win -Member $t -Namespace native

+ ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    + CategoryInfo          : InvalidData: (:) [Add-Type]，InvalidOperationException

    + FullyQualifiedErrorId : COMPILER_ERRORS,Microsoft.PowerShell.Commands.AddTypeCommand

## A

非常抱歉，這個錯誤訊息顯示我在上一版程式碼中，對於 `Add-Type` 的語法使用有誤。

**錯誤原因：** `Add-Type -Member` 這個參數不允許包含 `using System...` 這類的開頭宣告，它會自動把代碼包在一個類別裡，導致語法結構錯亂（C# 不允許在類別內部寫 `using`）。

請使用這個**完全修正的 V5 版本**。我改用了 `-TypeDefinition` 參數，這是最標準且不會報錯的寫法。

### 修正後的 AutoWallpaper.ps1 (V5)

請**全選**並**覆蓋**您原本的 `AutoWallpaper.ps1` 檔案內容：

```
# ==========================================
# V5 修正版：解決 Add-Type 編譯錯誤
# ==========================================

# 1. 啟動緩衝
Start-Sleep -Seconds 2

# 2. 定義 C# 代碼 (使用 TypeDefinition 完整定義，解決編譯錯誤)
$csharpSource = @"
using System;
using System.Runtime.InteropServices;

namespace Q7311Utils {
    public class WinAPI {
        // 設定桌布用的 API
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        // 隱藏視窗用的 API
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(int handle, int state);
    }
}
"@

# 載入定義
try {
    Add-Type -TypeDefinition $csharpSource -Language CSharp
} catch {
    # 如果已經載入過，忽略錯誤
}

# 3. 執行視窗隱藏 (雙重保險)
$hwnd = (Get-Process -Id $PID).MainWindowHandle
if ($hwnd -ne 0) { 
    [Q7311Utils.WinAPI]::ShowWindow($hwnd, 0) | Out-Null 
}

# 4. 載入螢幕偵測元件
try {
    Add-Type -AssemblyName System.Windows.Forms
} catch { }

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

# 5. 主迴圈
while ($true) {
    try {
        # 重新抓取螢幕資訊
        $Screen = [System.Windows.Forms.Screen]::PrimaryScreen
        $Width = $Screen.Bounds.Width
        $Height = $Screen.Bounds.Height
        
        # 判斷方向
        if ($Width -gt $Height) {
            $TargetFile = $LandscapePath
            $TargetState = "Landscape"
        } else {
            $TargetFile = $PortraitPath
            $TargetState = "Portrait"
        }

        # 狀態改變時執行
        if ($CurrentState -ne $TargetState) {
            if (Test-Path $TargetFile) {
                Set-WallpaperStyle
                # 執行換圖
                [void][Q7311Utils.WinAPI]::SystemParametersInfo($SPI_SETDESKWALLPAPER, 0, $TargetFile, $UpdateIniFile -bor $SendWinIniChange)
                
                $CurrentState = $TargetState
            }
        }
    } catch {
        # 忽略錯誤
    }
    
    Start-Sleep -Seconds 2
}

```

### 接下來該做什麼？

1.  **儲存檔案**：將上述內容存入 `C:\Wallpapers\AutoWallpaper.ps1`。
    
2.  **無需更改捷徑**：如果您已經依照上一步驟建立了帶有 `-WindowStyle Hidden` 的捷徑，**不需要重新建立捷徑**。
    
3.  **直接執行測試**：雙擊捷徑，這次不應該再出現紅字錯誤，視窗應該會順利隱藏，且旋轉功能應正常運作。
