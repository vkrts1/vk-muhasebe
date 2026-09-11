$sourceApp = "C:\Users\vkrts\Downloads\application-fc0b377a-ec96-4afe-aa79-a88931afcc2e (1)\VK"
$tempFolder = "C:\Users\vkrts\Downloads\IpaTemp"
$payloadFolder = "C:\Users\vkrts\Downloads\IpaTemp\Payload"
$targetAppFolder = "C:\Users\vkrts\Downloads\IpaTemp\Payload\VK.app"
$zipPath = "C:\Users\vkrts\Downloads\VK.zip"
$ipaDownloads = "C:\Users\vkrts\Downloads\VK.ipa"
$ipaDesktop = "C:\Users\vkrts\Desktop\VK.ipa"

if (Test-Path $tempFolder) { Remove-Item -Recurse -Force $tempFolder }
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }
if (Test-Path $ipaDownloads) { Remove-Item -Force $ipaDownloads }
if (Test-Path $ipaDesktop) { Remove-Item -Force $ipaDesktop }

New-Item -ItemType Directory -Path $targetAppFolder -Force | Out-Null

Get-ChildItem -Path $sourceApp | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $targetAppFolder -Recurse -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::CreateFromDirectory($tempFolder, $zipPath, [System.IO.Compression.CompressionLevel]::Optimal, $false)

Move-Item -Path $zipPath -Destination $ipaDownloads -Force
Copy-Item -Path $ipaDownloads -Destination $ipaDesktop -Force

Remove-Item -Recurse -Force $tempFolder

Write-Host "PAYLOAD_TAMAM: Payload klasorlu gercek IPA masaustunde hazirlandi!" -ForegroundColor Green
