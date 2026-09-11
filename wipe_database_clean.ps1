$ErrorActionPreference = "Stop"

Write-Host "==================================================="
Write-Host "  VK ON MUHASEBE - TEMIZ SIFIR VERITABANI ISLEMI"
Write-Host "==================================================="

# 1. Calisan VK sureclerini kapat
$procs = Get-Process -Name "VK" -ErrorAction SilentlyContinue
if ($procs) {
    Write-Host "Calisan VK uygulamasi kapatiliyor..."
    $procs | Stop-Process -Force
    Start-Sleep -Seconds 1
}

# 2. LocalAppData yolu
$appDataDir = "$env:LOCALAPPDATA\ErmayMuhasebe"
if (-not (Test-Path $appDataDir)) {
    New-Item -ItemType Directory -Path $appDataDir -Force | Out-Null
}

$backupsDir = Join-Path $appDataDir "Backups"
if (-not (Test-Path $backupsDir)) {
    New-Item -ItemType Directory -Path $backupsDir -Force | Out-Null
}

# 3. Mevcut veritabanini guvenlik icin yedekle
$dbFile = Join-Path $appDataDir "ErmayV4_Stable.db3"
if (Test-Path $dbFile) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupPath = Join-Path $backupsDir "yedek_sifirlama_oncesi_$timestamp.db3"
    Copy-Item $dbFile $backupPath -Force
    Write-Host "Mevcut veri guvenlik icin yedeklendi: $backupPath"
}

# 4. Firebase Cloud verilerini sifirla (Eski test verilerinin geri yuklenmesini onlemek icin)
$cloudConfigPath = "$env:LOCALAPPDATA\ermay_cloud_config.json"
if (Test-Path $cloudConfigPath) {
    try {
        $cfg = Get-Content $cloudConfigPath | ConvertFrom-Json
        if ($cfg.BaseUrl -and $cfg.AuthSecret) {
            Write-Host "Firebase Bulut veritabani temizleniyor ($($cfg.BaseUrl))..."
            $url2026 = "$($cfg.BaseUrl)/companies/default/years/2026.json?auth=$($cfg.AuthSecret)"
            Invoke-RestMethod -Uri $url2026 -Method Delete
            Write-Host "Firebase 2026 yili verileri (Cariler, Stoklar, Faturalar, vb.) tamamen temizlendi."
        }
    } catch {
        Write-Warning "Firebase temizleme uyarisi: $_"
    }
}

# 5. Yerel veritabani ve gecici dosyalari sil
$filesToDelete = @(
    "ErmayV4_Stable.db3",
    "ErmayV4_Stable.db3-wal",
    "ErmayV4_Stable.db3-shm",
    "notifications_*.json",
    "dismissed_alerts_*.json"
)

foreach ($f in $filesToDelete) {
    Get-ChildItem -Path $appDataDir -Filter $f -File -ErrorAction SilentlyContinue | ForEach-Object {
        Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
        Write-Host "Silindi: $($_.Name)"
    }
}

# 6. Temiz kurulum kullanici konfigurasyonu olustur (admin / 123)
$initUserConfig = @{
    Users = @(
        @{
            Username = "admin"
            Password = "123"
            Email = ""
        }
    )
    FactoryResetPassword = "123"
    GoogleClientId = ""
    GoogleClientSecret = ""
    SmtpEmail = ""
    SmtpPass = ""
    TelegramBotToken = ""
    TelegramChatId = ""
} | ConvertTo-Json -Depth 5

Set-Content -Path (Join-Path $appDataDir "setup_initial_user.json") -Value $initUserConfig -Encoding UTF8
Write-Host "Temiz yonetici (admin / 123) yapilandirmasi hazirlandi."

Write-Host "==================================================="
Write-Host "Veritabani basariyla tertemiz ve sifirlandi!"
Write-Host "Programi calistirdiginizda tertemiz, bos bir veritabani acilacaktir."
Write-Host "Giris Bilgileri: Kullanici: admin | Sifre: 123"
Write-Host "==================================================="
