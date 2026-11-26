[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Candidate,
    [Parameter(Mandatory)][version]$ExpectedVersion,
    [string]$PreviousSetup,
    [version]$PreviousVersion = '6.5.0'
)

$ErrorActionPreference = 'Stop'
$installerKey = 'HKLM:\Software\Kvieta'
if ($env:RUNNER_ENVIRONMENT -ne 'github-hosted' -or $env:GITHUB_ACTIONS -ne 'true' -or
    (Test-Path $installerKey) -or (Get-Service KvietaGuardian -ErrorAction SilentlyContinue)) {
    throw 'This acceptance test requires a fresh GitHub-hosted Windows runner.'
}

function Invoke-InstallerProcess([string]$path, [string[]]$arguments) {
    $process = Start-Process -FilePath (Resolve-Path -LiteralPath $path).Path -ArgumentList $arguments -PassThru
    try {
        if (-not $process.WaitForExit(120000)) { throw "Timed out waiting for $path." }
        return $process.ExitCode
    } finally {
        $process.Dispose()
    }
}

$fixture = Join-Path $env:RUNNER_TEMP 'kvieta-candidate'
New-Item -ItemType Directory -Path $fixture | Out-Null
if (-not $PreviousSetup) {
    $PreviousSetup = Join-Path $fixture 'Kvieta-Setup-Alpha-6.5.exe'
    Invoke-WebRequest -Uri 'https://github.com/Rel0adediso/Kvieta-app/releases/download/kvieta-alpha-6.5/Kvieta-Setup-Alpha-6.5.exe' -OutFile $PreviousSetup
    if ((Get-FileHash -LiteralPath $PreviousSetup).Hash -ne 'b3bbb4f7d58422b3e34e13ece5a6c2615d1a6dd1d1b94680ed9599f767dd7bc7') {
        throw 'The published Alpha 6.5 Setup did not match the pinned release hash.'
    }
}

$password = ConvertTo-SecureString ("Test!" + [guid]::NewGuid().ToString('N')) -AsPlainText -Force
$user = New-LocalUser -Name 'kvieta-candidate' -Password $password
$settings = Join-Path $fixture 'settings.json'
@{
    SchemaVersion = 10
    SetupCompleted = $true
    Mode = 'Protected'
    AdminPin = @{
        Version = 1
        Iterations = 210000
        SaltBase64 = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(16))
        HashBase64 = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    }
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $settings -Encoding utf8NoBOM
$request = @{ UserSid = $user.SID.Value; SettingsPath = $settings } | ConvertTo-Json -Compress
$payload = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($request))
$previousExit = Invoke-InstallerProcess $PreviousSetup @('--elevated-install', '--guardian-payload', $payload)
if ($previousExit -ne 0 -or (Get-Service KvietaGuardian).Status -ne 'Running') {
    throw "The previous Family installation failed (exit $previousExit)."
}
$previousProduct = Get-ItemProperty -LiteralPath $installerKey
if ([version]$previousProduct.InstalledVersion -ne $PreviousVersion) {
    throw "The fixture did not install version $PreviousVersion."
}

$programData = Join-Path $env:ProgramData 'Kvieta'
$protectedFiles = @($settings, (Join-Path $programData 'protected-settings.json'), (Join-Path $programData 'guardian-enrollment.json'))
$before = @(Get-FileHash -LiteralPath $protectedFiles)
$upgrade = Invoke-InstallerProcess $Candidate @('--elevated-install', '--preserve-installation', '--legacy-user-sid', $user.SID.Value)
$product = Get-ItemProperty -LiteralPath $installerKey
$application = Join-Path $env:ProgramFiles 'Kvieta/Kvieta.exe'
if ($upgrade -ne 0 -or [version]$product.InstalledVersion -ne $ExpectedVersion -or
    (Get-Item -LiteralPath $application).VersionInfo.FileVersion -ne "$ExpectedVersion.0" -or
    $product.ProductCode -eq $previousProduct.ProductCode -or
    (Test-Path "HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\$($previousProduct.ProductCode)") -or
    (Get-Service KvietaGuardian).Status -ne 'Running') {
    throw "Setup did not replace build $PreviousVersion with the candidate and restart Guardian (exit $upgrade)."
}
if (Compare-Object $before @(Get-FileHash -LiteralPath $protectedFiles) -Property Path, Hash) {
    throw "Upgrading from build $PreviousVersion changed the protected installation data."
}

$repair = Invoke-InstallerProcess $application @('--windows-admin-verification', '--audit-event', 'recovery.installer.repair')
if ($repair -ne 0 -or (Get-Service KvietaGuardian).Status -ne 'Running') {
    throw "The Windows administrator recovery repair failed (exit $repair)."
}
if (Compare-Object $before @(Get-FileHash -LiteralPath $protectedFiles) -Property Path, Hash) {
    throw 'Administrator recovery repair changed protected installation data.'
}

$logPath = Join-Path $env:RUNNER_TEMP 'kvieta-candidate-uninstall.log'
$uninstall = Invoke-InstallerProcess "$env:SystemRoot/System32/msiexec.exe" @('/x', $product.ProductCode, '/qn', '/norestart', '/L*v', ('"' + $logPath + '"'))
if ($uninstall -ne 0 -or (Test-Path -LiteralPath $installerKey) -or
    (Get-Service KvietaGuardian -ErrorAction SilentlyContinue) -or (Test-Path -LiteralPath $application)) {
    throw "The MSI uninstall did not remove the candidate and Guardian (exit $uninstall)."
}
foreach ($task in 'Kvieta Prepare Update', 'Kvieta Apply Update', 'Kvieta Install Update') {
    if (Get-ScheduledTask -TaskName $task -ErrorAction SilentlyContinue) { throw "Uninstall left the $task scheduled task." }
}
Write-Output "The packaged candidate upgraded build $PreviousVersion, passed administrator repair, and uninstalled."
