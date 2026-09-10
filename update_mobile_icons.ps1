Add-Type -AssemblyName System.Drawing

$src = 'C:\Users\vkrts\.gemini\antigravity-ide\brain\f5a15436-05b5-4440-b16f-fce80d80093c\.user_uploaded\media_1788633043875.png'
$mobilAssets = 'f:\avalonia yedek\ermaymuhasebe\ermaymuhasebe-mobil\assets'

# Ana ikonlar
Copy-Item -Path $src -Destination "$mobilAssets\icon.png" -Force
Copy-Item -Path $src -Destination "$mobilAssets\adaptive-icon.png" -Force
Copy-Item -Path $src -Destination "$mobilAssets\favicon.png" -Force
Copy-Item -Path $src -Destination "$mobilAssets\splash-icon.png" -Force

# Arka plan oluştur (1024x1024 siyah)
$bgBmp = New-Object System.Drawing.Bitmap 1024, 1024
$bgGraphics = [System.Drawing.Graphics]::FromImage($bgBmp)
$bgGraphics.Clear([System.Drawing.Color]::Black)
$bgBmp.Save("$mobilAssets\android-icon-background.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bgGraphics.Dispose()
$bgBmp.Dispose()

# Ön plan oluştur (1024x1024)
$img = [System.Drawing.Image]::FromFile($src)
$fgBmp = New-Object System.Drawing.Bitmap 1024, 1024
$fgGraphics = [System.Drawing.Graphics]::FromImage($fgBmp)
$fgGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$fgGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$fgGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

$fgGraphics.DrawImage($img, 0, 0, 1024, 1024)
$fgBmp.Save("$mobilAssets\android-icon-foreground.png", [System.Drawing.Imaging.ImageFormat]::Png)
$fgGraphics.Dispose()
$fgBmp.Dispose()
$img.Dispose()

Write-Host "Mobil ikonlar basariyla guncellendi!"
