$ErrorActionPreference = 'Stop'
$check = Join-Path $PSScriptRoot '../scripts/assert-stable-feed-advance.ps1'
$key = [Security.Cryptography.RSA]::Create(2048)
try {
    $publicKey = [Convert]::ToBase64String($key.ExportSubjectPublicKeyInfo())
    function New-Envelope([string]$version, [string]$hash = 'A', [string]$product = 'Kvieta', [string]$kind = 'setup', [int]$revision = 1) {
        $metadata = @{ Product = $product; Architecture = 'win-x64'; PackageKind = $kind; Version = $version; Sha256 = $hash; GuardianRevision = $revision }
        $payload = [Text.Encoding]::UTF8.GetBytes(($metadata | ConvertTo-Json -Compress))
        $signature = $key.SignData($payload, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1)
        return (@{ Payload = [Convert]::ToBase64String($payload); Signature = [Convert]::ToBase64String($signature) } | ConvertTo-Json -Compress)
    }
    function Should-Reject([scriptblock]$action) {
        $rejected = $false
        try { & $action } catch { $rejected = $true }
        if (-not $rejected) { throw 'An invalid stable feed advance was accepted.' }
    }
    $old = New-Envelope '7.0.1'
    $current = New-Envelope '7.0.2'
    & $check -PreviousEnvelope $old -NextEnvelope $current -PublicKey $publicKey -ExpectedVersion '7.0.2'
    & $check -PreviousEnvelope $current -NextEnvelope $current -PublicKey $publicKey -ExpectedVersion '7.0.2'
    Should-Reject { & $check -PreviousEnvelope $current -NextEnvelope $old -PublicKey $publicKey -ExpectedVersion '7.0.1' }
    $conflict = New-Envelope '7.0.2' 'B'
    Should-Reject { & $check -PreviousEnvelope $current -NextEnvelope $conflict -PublicKey $publicKey -ExpectedVersion '7.0.2' }
    $forged = $current | ConvertFrom-Json
    $forged.Signature = [Convert]::ToBase64String([byte[]]::new(256))
    $forgedJson = $forged | ConvertTo-Json -Compress
    Should-Reject { & $check -PreviousEnvelope $old -NextEnvelope $forgedJson -PublicKey $publicKey -ExpectedVersion '7.0.2' }
    Should-Reject { & $check -PreviousEnvelope $forgedJson -NextEnvelope $current -PublicKey $publicKey -ExpectedVersion '7.0.2' }
    Should-Reject { & $check -PreviousEnvelope $old -NextEnvelope $current -PublicKey $publicKey -ExpectedVersion '7.0.3' }
    $otherProduct = New-Envelope '7.0.3' 'A' 'Other'
    Should-Reject { & $check -PreviousEnvelope $current -NextEnvelope $otherProduct -PublicKey $publicKey -ExpectedVersion '7.0.3' }
    $wrongKind = New-Envelope '7.0.3' 'A' 'Kvieta' 'msi'
    Should-Reject { & $check -PreviousEnvelope $current -NextEnvelope $wrongKind -PublicKey $publicKey -ExpectedVersion '7.0.3' }
    $guardianRelease = New-Envelope '7.0.3' -revision 2
    & $check -PreviousEnvelope $current -NextEnvelope $guardianRelease -PublicKey $publicKey -ExpectedVersion '7.0.3'
    $mislabeledRelease = New-Envelope '7.0.4'
    Should-Reject { & $check -PreviousEnvelope $guardianRelease -NextEnvelope $mislabeledRelease -PublicKey $publicKey -ExpectedVersion '7.0.4' }
    Write-Output 'Stable feed advance accepts valid updates and rejects rollback, version collisions, forgery and wrong products.'
} finally { $key.Dispose() }
