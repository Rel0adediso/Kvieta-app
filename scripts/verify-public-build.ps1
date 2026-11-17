[CmdletBinding()]
param(
    [string]$AssemblyPath = 'src\Kvieta.App\bin\Release\net10.0-windows\Kvieta.dll'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$resolvedAssemblyPath = if ([System.IO.Path]::IsPathRooted($AssemblyPath)) {
    $AssemblyPath
} else {
    Join-Path $repositoryRoot $AssemblyPath
}

if (-not (Test-Path -LiteralPath $resolvedAssemblyPath -PathType Leaf)) {
    throw "Public assembly was not found: $resolvedAssemblyPath"
}

function Get-HexRepresentation([byte[]]$data) {
    $toHexMethod = [System.Convert].GetMethod('ToHexString', [Type[]]@([byte[]]))
    if ($null -ne $toHexMethod) {
        return [System.Convert]::ToHexString($data)
    }
    return [System.BitConverter]::ToString($data).Replace('-', '')
}

$bytes = [System.IO.File]::ReadAllBytes($resolvedAssemblyPath)
$assemblyHex = Get-HexRepresentation $bytes
$forbiddenMarkers = @(
    'ForceUnlockForTestingAsync',
    'ForceApplyPendingForTestingAsync',
    'Test bypass',
    'Test atlaması',
    'KVIETA_DEVELOPMENT_BUILD'
)

foreach ($marker in $forbiddenMarkers) {
    $patterns = @(
        [System.Text.Encoding]::UTF8.GetBytes($marker),
        [System.Text.Encoding]::Unicode.GetBytes($marker)
    )

    foreach ($pattern in $patterns) {
        $patternHex = Get-HexRepresentation $pattern
        if ($assemblyHex.IndexOf($patternHex, [System.StringComparison]::Ordinal) -ge 0) {
            throw "Development-only marker '$marker' was found in the public assembly."
        }
    }
}

Write-Output "Public build verification passed: $resolvedAssemblyPath"
