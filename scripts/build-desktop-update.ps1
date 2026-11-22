[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^\d+\.\d+\.\d+$')][string]$Version,
    [string]$Vpk = 'vpk',
    [string]$OutputDirectory = 'artifacts/desktop-releases',
    [switch]$FrameworkDependent
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$output = [IO.Path]::GetFullPath($OutputDirectory, $repo)
# Always publish to a fresh directory. Reusing a version directory can silently
# include files that were removed from the project since a previous build.
$publish = Join-Path $repo "artifacts/desktop-publish/$Version-$([guid]::NewGuid().ToString('N'))"
$publicKey = $env:KVIETA_DESKTOP_PUBLIC_KEY
if ([string]::IsNullOrWhiteSpace($publicKey) -or [string]::IsNullOrWhiteSpace($env:KVIETA_DESKTOP_PRIVATE_KEY)) {
    throw 'Desktop release signing keys are required.'
}

# The native delta tool cannot handle all Unicode temporary paths on Windows.
$oldTemp = $env:TEMP
$oldTmp = $env:TMP
$nativeTemp = $null
try {
    $selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
    dotnet publish (Join-Path $repo 'src/Kvieta.Desktop/Kvieta.Desktop.csproj') -c Release -r win-x64 --self-contained $selfContained `
        "-p:Version=$Version" "-p:KvietaDesktopPublicKey=$publicKey" "-p:PublishDir=$publish/" -m:1
    if ($LASTEXITCODE -ne 0) { throw 'Desktop publish failed.' }

    if ($env:TEMP -match '[^\x00-\x7F]') {
        $nativeTemp = New-Item -ItemType Directory -Path (Join-Path $env:SystemRoot "Temp/kvieta-vpk-$([guid]::NewGuid().ToString('N'))")
        $env:TEMP = $nativeTemp.FullName
        $env:TMP = $nativeTemp.FullName
    }
    & $Vpk pack --packId Kvieta.Desktop --packVersion $Version --packDir $publish --mainExe Kvieta.Desktop.exe `
        --packTitle 'Kvieta Desktop' --channel desktop-stable --runtime win-x64 --outputDir $output `
        --icon (Join-Path $repo 'assets/branding/kvieta.ico')
    if ($LASTEXITCODE -ne 0) { throw 'Desktop packaging failed.' }
} finally {
    $env:TEMP = $oldTemp
    $env:TMP = $oldTmp
    if ($nativeTemp -and $nativeTemp.Parent.FullName -eq (Join-Path $env:SystemRoot 'Temp') -and
        $nativeTemp.Name -match '^kvieta-vpk-[a-f0-9]{32}$') {
        Remove-Item -LiteralPath $nativeTemp.FullName -Recurse -Force
    }
    $publishItem = Get-Item -LiteralPath $publish -ErrorAction SilentlyContinue
    if ($publishItem -and $publishItem.Parent.FullName -eq [IO.Path]::GetFullPath((Join-Path $repo 'artifacts/desktop-publish')) -and
        $publishItem.Name -match '^\d+\.\d+\.\d+-[a-f0-9]{32}$') {
        Remove-Item -LiteralPath $publishItem.FullName -Recurse -Force
    }
}

$feed = Join-Path $output 'releases.desktop-stable.json'
$allAssets = @((Get-Content -LiteralPath $feed -Raw | ConvertFrom-Json).Assets)
# Keep a short delta chain; clients further behind fall back to the latest full
# package. Otherwise the signed content catalog eventually exceeds client limits.
$versions = @($allAssets | ForEach-Object { [version]$_.Version } | Sort-Object -Unique -Descending | Select-Object -First 3)
$assets = @($allAssets | Where-Object { [version]$_.Version -in $versions })
if ($assets.Count -eq 0 -or -not ($assets | Where-Object { $_.Type -eq 'Full' -and $_.Version -eq $Version })) {
    throw 'The desktop feed does not contain the current full package.'
}
foreach ($asset in $assets) {
    if ($asset.PackageId -cne 'Kvieta.Desktop' -or $asset.FileName -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,200}\.nupkg$') {
        throw 'Unexpected desktop package.'
    }
    $file = Join-Path $output $asset.FileName
    if ((Get-Item -LiteralPath $file).Length -ne $asset.Size -or (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash -ne $asset.SHA256) {
        throw 'Desktop package metadata did not match its file.'
    }
}
$contents = foreach ($asset in $assets | Where-Object { $_.Type -eq 'Full' }) {
    $archive = [IO.Compression.ZipFile]::OpenRead((Join-Path $output $asset.FileName))
    try {
        $entries = foreach ($entry in $archive.Entries) {
            $stream = $entry.Open()
            try { @{ Name = $entry.FullName; Length = $entry.Length; Sha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) } }
            finally { $stream.Dispose() }
        }
        @{ FileName = $asset.FileName; Entries = @($entries) }
    } finally { $archive.Dispose() }
}
$bytes = [Text.Encoding]::UTF8.GetBytes((@{ Assets = @($assets); Contents = @($contents) } | ConvertTo-Json -Depth 8 -Compress))
$rsa = [Security.Cryptography.RSA]::Create()
try {
    $used = 0
    $rsa.ImportPkcs8PrivateKey([Convert]::FromBase64String($env:KVIETA_DESKTOP_PRIVATE_KEY), [ref]$used)
    if ($rsa.KeySize -lt 2048 -or [Convert]::ToBase64String($rsa.ExportSubjectPublicKeyInfo()) -cne $publicKey) {
        throw 'Desktop signing key does not match the embedded public key.'
    }
    $signature = $rsa.SignData($bytes, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1)
    $envelope = @{ Payload = [Convert]::ToBase64String($bytes); Signature = [Convert]::ToBase64String($signature) }
    $signedFeed = $envelope | ConvertTo-Json -Compress
    if ([Text.Encoding]::UTF8.GetByteCount($signedFeed) -gt 1MB) {
        throw 'The signed desktop feed exceeds the client download limit.'
    }
    [IO.File]::WriteAllText((Join-Path $output 'desktop.json'), $signedFeed)
} finally { $rsa.Dispose() }
Write-Output "Signed desktop package feed is ready: $output"
