$projectDir = $PSScriptRoot
$srcPng = Join-Path $projectDir "ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia\Assets\vk_logo_master.png"

# Copy to Avalonia Assets as vk_logo.png and app_icon.png
Copy-Item $srcPng (Join-Path $projectDir "ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia\Assets\vk_logo.png") -Force
Copy-Item $srcPng (Join-Path $projectDir "ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia\Assets\app_icon.png") -Force

Add-Type -AssemblyName System.Drawing

function Convert-PngToMultiSizeIco($sourcePngPath, $destIcoPath) {
    $sizes = @(16, 24, 32, 48, 64, 128, 256)
    $srcImg = [System.Drawing.Image]::FromFile($sourcePngPath)
    
    $msList = New-Object System.Collections.Generic.List[System.IO.MemoryStream]
    
    foreach ($size in $sizes) {
        $bmp = New-Object System.Drawing.Bitmap $size, $size
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.DrawImage($srcImg, 0, 0, $size, $size)
        $g.Dispose()
        
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        $msList.Add($ms)
        $bmp.Dispose()
    }
    
    $fs = New-Object System.IO.FileStream $destIcoPath, ([System.IO.FileMode]::Create)
    $bw = New-Object System.IO.BinaryWriter $fs
    
    # ICONDIR Header: Reserved(2), Type(2, 1=Icon), Count(2)
    $bw.Write([UInt16]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]$sizes.Count)
    
    $offset = 6 + ($sizes.Count * 16)
    
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $size = $sizes[$i]
        $w = if ($size -ge 256) { 0 } else { [byte]$size }
        $h = if ($size -ge 256) { 0 } else { [byte]$size }
        $bytes = $msList[$i].ToArray()
        
        # ICONDIRENTRY: Width, Height, ColorCount, Reserved, Planes, BitCount, BytesInRes, ImageOffset
        $bw.Write([byte]$w)
        $bw.Write([byte]$h)
        $bw.Write([byte]0)
        $bw.Write([byte]0)
        $bw.Write([UInt16]1)
        $bw.Write([UInt16]32)
        $bw.Write([UInt32]$bytes.Length)
        $bw.Write([UInt32]$offset)
        
        $offset += $bytes.Length
    }
    
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $bytes = $msList[$i].ToArray()
        $bw.Write($bytes)
        $msList[$i].Dispose()
    }
    
    $bw.Flush()
    $bw.Close()
    $fs.Close()
    $srcImg.Dispose()
    Write-Host "Multi-size ICO olusturuldu: $destIcoPath"
}

Convert-PngToMultiSizeIco $srcPng (Join-Path $projectDir "ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia\Assets\avalonia-logo.ico")
Convert-PngToMultiSizeIco $srcPng (Join-Path $projectDir "ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia\Assets\app_icon.ico")
Convert-PngToMultiSizeIco $srcPng (Join-Path $projectDir "ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia.Desktop\Assets\avalonia-logo.ico")
Convert-PngToMultiSizeIco $srcPng (Join-Path $projectDir "ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia.Desktop\Assets\app.ico")
Write-Host "Tum VK ikonlari guncellendi!"
