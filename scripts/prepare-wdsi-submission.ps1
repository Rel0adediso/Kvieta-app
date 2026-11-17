[CmdletBinding()]
param(
    [string]$ExecutablePath = "artifacts/publish-community/win-x64-v14/Kvieta.exe",
    [switch]$OpenBrowser
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

$exeFullPath = Join-Path $repoRoot $ExecutablePath
if (-not (Test-Path -LiteralPath $exeFullPath)) {
    throw "Executable not found at: $exeFullPath"
}

$outputDir = Join-Path $repoRoot "artifacts/wdsi-submission"
if (-not (Test-Path -LiteralPath $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$sha256 = (Get-FileHash -LiteralPath $exeFullPath -Algorithm SHA256).Hash
$version = "6.5.0"
$propsPath = Join-Path $repoRoot "Directory.Build.props"
if (Test-Path -LiteralPath $propsPath) {
    $propsContent = Get-Content -LiteralPath $propsPath -Raw
    if ($propsContent -match '<Version>([^<]+)</Version>') {
        $version = $matches[1]
    }
}

$zipName = "Kvieta-v$version-wdsi.zip"
$zipPath = Join-Path $outputDir $zipName
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Compress-Archive -Path $exeFullPath -DestinationPath $zipPath -Force

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " Microsoft Defender Security Intelligence (WDSI) Submission " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "1. Hazırlanan ZIP Dosyası : " -NoNewline; Write-Host $zipPath -ForegroundColor Green
Write-Host "2. SHA-256 Özeti          : " -NoNewline; Write-Host $sha256 -ForegroundColor Yellow
Write-Host "3. Ürün Adı (Product)     : Kvieta" -ForegroundColor White
Write-Host "4. Şirket / Geliştirici   : Rel0adediso / Kvieta" -ForegroundColor White
Write-Host "5. Versiyon               : $version" -ForegroundColor White
Write-Host ""
Write-Host "--- Açıklama Metni (WDSI formuna yapıştırılacak) ---" -ForegroundColor Cyan
$comments = @"
Product: Kvieta (v$version)
Repository: https://github.com/Rel0adediso/Kvieta-app
Description: Kvieta is an open-source Windows digital wellness, focus, and local screen-time management application.
Request: We are submitting our official release binary for proactive software developer verification, false-positive prevention, and Microsoft Defender SmartScreen reputation whitelisting. The binary is clean and safe.
SHA-256: $sha256
"@
Write-Host $comments -ForegroundColor Gray
Write-Host "----------------------------------------------------" -ForegroundColor Cyan
Write-Host ""
Write-Host "Portal Adresi: https://www.microsoft.com/en-us/wdsi/filesubmission" -ForegroundColor Yellow

if ($OpenBrowser) {
    Start-Process "https://www.microsoft.com/en-us/wdsi/filesubmission"
}
