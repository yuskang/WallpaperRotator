# PowerShell script to generate app.ico from SVG
# Run this on Windows with .NET installed

param(
    [string]$SvgPath = "src/WallpaperRotator/Resources/app-icon.svg",
    [string]$OutputPath = "src/WallpaperRotator/Resources/app.ico"
)

Add-Type -AssemblyName System.Drawing

# Icon sizes for Windows ICO
$sizes = @(16, 32, 48, 64, 128, 256)

# Create a simple icon programmatically (since SVG conversion is complex)
# This creates a modern gradient icon with "WR" text

function Create-IconBitmap {
    param([int]$size)

    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

    # Background gradient (blue)
    $rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(0, 120, 212),  # #0078D4
        [System.Drawing.Color]::FromArgb(0, 90, 158),   # #005A9E
        [System.Drawing.Drawing2D.LinearGradientMode]::ForwardDiagonal
    )

    # Rounded rectangle path
    $cornerRadius = [int]($size * 0.2)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(0, 0, $cornerRadius * 2, $cornerRadius * 2, 180, 90)
    $path.AddArc($size - $cornerRadius * 2, 0, $cornerRadius * 2, $cornerRadius * 2, 270, 90)
    $path.AddArc($size - $cornerRadius * 2, $size - $cornerRadius * 2, $cornerRadius * 2, $cornerRadius * 2, 0, 90)
    $path.AddArc(0, $size - $cornerRadius * 2, $cornerRadius * 2, $cornerRadius * 2, 90, 90)
    $path.CloseFigure()

    $graphics.FillPath($brush, $path)

    # Draw "WR" text
    $fontSize = [int]($size * 0.35)
    $font = New-Object System.Drawing.Font("Segoe UI", $fontSize, [System.Drawing.FontStyle]::Bold)
    $textBrush = [System.Drawing.Brushes]::White

    $textSize = $graphics.MeasureString("WR", $font)
    $x = ($size - $textSize.Width) / 2
    $y = ($size - $textSize.Height) / 2

    $graphics.DrawString("WR", $font, $textBrush, $x, $y)

    # Draw rotation arrow at bottom
    if ($size -ge 48) {
        $arrowPen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, [int]($size * 0.04))
        $arrowPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $arrowPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

        $centerX = $size / 2
        $centerY = $size * 0.75
        $radius = $size * 0.15

        $arcRect = New-Object System.Drawing.RectangleF(
            ($centerX - $radius), ($centerY - $radius),
            ($radius * 2), ($radius * 2)
        )
        $graphics.DrawArc($arrowPen, $arcRect, 180, 270)

        $arrowPen.Dispose()
    }

    $graphics.Dispose()
    $brush.Dispose()
    $font.Dispose()
    $path.Dispose()

    return $bitmap
}

# Create multi-size icon
$iconImages = @()
foreach ($size in $sizes) {
    $iconImages += Create-IconBitmap -size $size
}

# Save as ICO using the largest image
$largestBitmap = $iconImages[-1]
$iconHandle = $largestBitmap.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($iconHandle)

# Save to file
$fileStream = [System.IO.File]::Create($OutputPath)
$icon.Save($fileStream)
$fileStream.Close()

# Cleanup
foreach ($img in $iconImages) {
    $img.Dispose()
}

Write-Host "Icon generated successfully: $OutputPath"
