[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PreviousEnvelope,
    [Parameter(Mandatory)][string]$NextEnvelope,
    [Parameter(Mandatory)][string]$PublicKey,
    [Parameter(Mandatory)][version]$ExpectedVersion
)

$ErrorActionPreference = 'Stop'
$previous = $PreviousEnvelope | ConvertFrom-Json
$next = $NextEnvelope | ConvertFrom-Json
$rsa = [Security.Cryptography.RSA]::Create()
try {
    $keyBytes = [Convert]::FromBase64String($PublicKey)
    $read = 0
    $rsa.ImportSubjectPublicKeyInfo($keyBytes, [ref]$read)
    if ($read -ne $keyBytes.Length -or $rsa.KeySize -lt 2048) { throw 'Invalid stable public key.' }
    foreach ($feed in @($previous, $next)) {
        $payload = [Convert]::FromBase64String($feed.Payload)
        $signature = [Convert]::FromBase64String($feed.Signature)
        if (-not $rsa.VerifyData($payload, $signature, [Security.Cryptography.HashAlgorithmName]::SHA256, [Security.Cryptography.RSASignaturePadding]::Pkcs1)) {
            throw 'Stable feed signature is invalid.'
        }
    }
} finally { $rsa.Dispose() }

$previousMetadata = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($previous.Payload)) | ConvertFrom-Json
$nextMetadata = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($next.Payload)) | ConvertFrom-Json
if ($previousMetadata.Product -cne 'Kvieta' -or $nextMetadata.Product -cne 'Kvieta' -or
    $previousMetadata.Architecture -cne 'win-x64' -or $nextMetadata.Architecture -cne 'win-x64' -or
    $previousMetadata.PackageKind -cne 'setup' -or $nextMetadata.PackageKind -cne 'setup') {
    throw 'Unexpected stable feed product, architecture or package kind.'
}
$previousVersion = [version]$previousMetadata.Version
$nextVersion = [version]$nextMetadata.Version
if ($previousMetadata.GuardianRevision -isnot [long] -or $nextMetadata.GuardianRevision -isnot [long] -or
    $previousMetadata.GuardianRevision -lt 1 -or $nextMetadata.GuardianRevision -gt [int]::MaxValue -or
    $nextMetadata.GuardianRevision -lt $previousMetadata.GuardianRevision) {
    throw 'The Guardian revision must stay the same or increase.'
}
if ($nextVersion -ne $ExpectedVersion) { throw 'The new stable feed has an unexpected version.' }
if ($previousVersion -gt $nextVersion) { throw 'Refusing to replace a newer stable feed.' }
if ($previousVersion -eq $nextVersion -and $previous.Payload -cne $next.Payload) {
    throw 'A different update already uses this version.'
}
