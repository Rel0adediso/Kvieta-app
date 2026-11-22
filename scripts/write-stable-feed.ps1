[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Setup,
    [Parameter(Mandatory)][version]$Version,
    [Parameter(Mandatory)][ValidatePattern('^v\d+\.\d+\.\d+$')][string]$Label,
    [Parameter(Mandatory)][string]$OutputPath
)
$ErrorActionPreference = 'Stop'
[xml]$build = Get-Content -LiteralPath (Join-Path $PSScriptRoot '../Directory.Build.props') -Raw
$guardianRevision = 0
if (-not [int]::TryParse($build.Project.PropertyGroup.KvietaGuardianRevision, [ref]$guardianRevision) -or $guardianRevision -lt 1) {
    throw 'A positive Guardian revision is required in Directory.Build.props.'
}
if (-not ('Kvieta.App.Services.StableChunker' -as [type])) {
    Add-Type -Path (Join-Path $PSScriptRoot '../src/Kvieta.App/Services/StableChunker.cs')
}
$file = Get-Item -LiteralPath $Setup
if ($file.Name -cne "Kvieta-Setup-$Label.exe" -or $file.Length -le 0 -or
    $file.VersionInfo.FileVersion -ne "$Version.0" -or $file.VersionInfo.ProductVersion -cne $Label) {
    throw 'Stable setup name, file version or release label does not match.'
}
if ($file.Length -gt 256MB) { throw 'Stable installer exceeds the client download limit.' }
$rsa = [Security.Cryptography.RSA]::Create()
$hash = [Security.Cryptography.SHA256]::Create()
$stream = $file.OpenRead()
try {
    $used = 0
    $rsa.ImportPkcs8PrivateKey([Convert]::FromBase64String($env:KVIETA_STABLE_PRIVATE_KEY), [ref]$used)
    if ($rsa.KeySize -lt 2048 -or [Convert]::ToBase64String($rsa.ExportSubjectPublicKeyInfo()) -cne $env:KvietaStablePublicKey) {
        throw 'Stable signing key does not match the public key embedded in this build.'
    }
    $blocks = [Collections.Generic.List[string]]::new()
    $sizes = [Collections.Generic.List[int]]::new()
    $buffer = [byte[]]::new([Kvieta.App.Services.StableChunker]::Maximum)
    while ($stream.Position -lt $stream.Length) {
        $count = [Kvieta.App.Services.StableChunker]::ReadChunk($stream, $buffer)
        $sizes.Add($count)
        $blocks.Add([Convert]::ToHexString($hash.ComputeHash($buffer, 0, $count)))
    }
    $stream.Position = 0
    $metadata = [ordered]@{
        Product = 'Kvieta'; Architecture = 'win-x64'; PackageKind = 'setup'; Version = $Version.ToString(); Label = $Label
        GuardianRevision = $guardianRevision
        Size = $stream.Length; Sha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)); Blocks = $blocks; Sizes = $sizes
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes(($metadata | ConvertTo-Json -Compress))
    $signature = $rsa.SignData($bytes, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1)
    $envelope = @{ Payload = [Convert]::ToBase64String($bytes); Signature = [Convert]::ToBase64String($signature) }
    [IO.File]::WriteAllText($OutputPath, ($envelope | ConvertTo-Json -Compress))
} finally {
    $rsa.Dispose()
    $hash.Dispose()
    $stream.Dispose()
}
