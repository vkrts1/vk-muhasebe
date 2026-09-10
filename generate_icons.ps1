$src = "C:\Users\vkrts\.gemini\antigravity-ide\brain\f5a15436-05b5-4440-b16f-fce80d80093c\.user_uploaded\media_1788633043875.png"

# Hedef klasörlerin varlığını kontrol et ve oluştur
$targetDirs = @(
    "f:\avalonia yedek\ermaymuhasebe\ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia\Assets",
    "f:\avalonia yedek\ermaymuhasebe\ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia.Desktop\Assets",
    "f:\avalonia yedek\ermaymuhasebe\ermaymuhasebe-mobil\assets"
)

foreach ($dir in $targetDirs) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}

# PNG dosyalarını kopyala
Copy-Item -Path $src -Destination "f:\avalonia yedek\ermaymuhasebe\ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia\Assets\app_icon.png" -Force
Copy-Item -Path $src -Destination "f:\avalonia yedek\ermaymuhasebe\ermaymuhasebe-mobil\assets\icon.png" -Force
Copy-Item -Path $src -Destination "f:\avalonia yedek\ermaymuhasebe\ermaymuhasebe-mobil\assets\adaptive-icon.png" -Force
Copy-Item -Path $src -Destination "f:\avalonia yedek\ermaymuhasebe\ermaymuhasebe-mobil\assets\favicon.png" -Force

# .NET System.Drawing ile .ico dosyası üret
Add-Type -AssemblyName System.Drawing
$png = [System.Drawing.Bitmap]::FromFile($src)
$thumb = New-Object System.Drawing.Bitmap $png, (New-Object System.Drawing.Size 256, 256)
$hIcon = $thumb.GetHicon()
$ico = [System.Drawing.Icon]::FromHandle($hIcon)

$paths = @(
    "f:\avalonia yedek\ermaymuhasebe\ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia\Assets\app_icon.ico",
    "f:\avalonia yedek\ermaymuhasebe\ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia\Assets\avalonia-logo.ico",
    "f:\avalonia yedek\ermaymuhasebe\ErmayMuhasebe.Avalonia\ErmayMuhasebe.Avalonia.Desktop\Assets\avalonia-logo.ico"
)

foreach ($p in $paths) {
    $fs = New-Object System.IO.FileStream $p, ([System.IO.FileMode]::Create)
    $ico.Save($fs)
    $fs.Close()
}

$png.Dispose()
$thumb.Dispose()
$ico.Dispose()
Write-Host "Masaustu ve Mobil icin yeni 'B' amblemli logo ve .ico basariyla olusturuldu!"
