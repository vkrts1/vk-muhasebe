Add-Type -AssemblyName System.Drawing
$png = 'f:\avalonia yedek\ermaymuhasebe\ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia\Assets\app_icon.png'
$ico = 'f:\avalonia yedek\ermaymuhasebe\ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia\Assets\app_icon.ico'
$bmp = [System.Drawing.Bitmap]::FromFile($png)
$icon = [System.Drawing.Icon]::FromHandle($bmp.GetHicon())
$fs = [System.IO.File]::Create($ico)
$icon.Save($fs)
$fs.Close()
$bmp.Dispose()
Write-Host "ICO_SUCCESS"
